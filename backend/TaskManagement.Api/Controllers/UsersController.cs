using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Data;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Models;
using TaskManagement.Api.Services;

namespace TaskManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ITaskAccessService _access;

        public UsersController(AppDbContext db, ITaskAccessService access)
        {
            _db = db;
            _access = access;
        }

        // Admin: all active users. Manager: active users with the User role (to add into a team).
        [HttpGet]
        [Authorize(Roles = AppRoles.AdminOrManager)]
        public async Task<IActionResult> GetUsers()
        {
            IQueryable<AppUser> query = _db.Users.Where(u => u.IsActive);

            if (!User.IsInRole(AppRoles.Admin))
                query = query.Where(u => u.Role == AppRoles.User);

            var users = await query
                .OrderBy(u => u.FullName)
                .Select(UserResponse.Projection)
                .ToListAsync();

            return Ok(users);
        }

        // Users the caller is allowed to assign tasks to.
        [HttpGet("assignable")]
        [Authorize(Roles = AppRoles.AdminOrManager)]
        public async Task<IActionResult> GetAssignable()
        {
            var userId = _access.GetUserId(User);

            IQueryable<AppUser> query = _db.Users.Where(u => u.IsActive);

            if (User.IsInRole(AppRoles.Admin))
            {
                query = query.Where(u =>
                    u.Role == AppRoles.Manager || u.Role == AppRoles.User);
            }
            else
            {
                query = query.Where(u =>
                    u.TeamId != null &&
                    _db.Teams.Any(t => t.Id == u.TeamId && t.ManagerId == userId));
            }

            var users = await query
                .OrderBy(u => u.FullName)
                .Select(UserResponse.Projection)
                .ToListAsync();

            return Ok(users);
        }

        [HttpPatch("{id:int}/role")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> ChangeRole(int id, ChangeRoleRequest request)
        {
            if (!AppRoles.All.Contains(request.Role))
                return BadRequest(new { message = "Invalid role." });

            if (id == _access.GetUserId(User))
                return BadRequest(new { message = "You cannot change your own role." });

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound(new { message = "User not found." });

            user.Role = request.Role;

            if (request.Role != AppRoles.Manager)
            {
                await _db.Teams
                    .Where(t => t.ManagerId == id)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.ManagerId, t => (int?)null));
            }

            await _db.SaveChangesAsync();

            var response = await _db.Users
                .Where(u => u.Id == id)
                .Select(UserResponse.Projection)
                .FirstAsync();

            return Ok(response);
        }
    }
}