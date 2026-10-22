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
    public class TeamsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ITaskAccessService _access;

        public TeamsController(AppDbContext db, ITaskAccessService access)
        {
            _db = db;
            _access = access;
        }

        private IActionResult Denied(string message) =>
            StatusCode(StatusCodes.Status403Forbidden, new { message });

        private Task<TeamResponse?> LoadTeamAsync(int id) =>
            _db.Teams
                .Where(t => t.Id == id)
                .Select(TeamResponse.Projection)
                .FirstOrDefaultAsync();

        private async Task<string?> ValidateManagerAsync(int? managerId)
        {
            if (managerId == null) return null;

            var manager = await _db.Users.FirstOrDefaultAsync(u =>
                u.Id == managerId && u.IsActive);

            if (manager == null) return "Manager not found.";
            if (manager.Role != AppRoles.Manager)
                return "Selected user does not have the Manager role.";

            return null;
        }

        // Admin: all teams. Manager: teams they manage. User: their own team.
        [HttpGet]
        public async Task<IActionResult> GetTeams()
        {
            var userId = _access.GetUserId(User);
            IQueryable<Team> query = _db.Teams;

            if (User.IsInRole(AppRoles.Manager))
            {
                query = query.Where(t => t.ManagerId == userId);
            }
            else if (!User.IsInRole(AppRoles.Admin))
            {
                var teamId = await _db.Users
                    .Where(u => u.Id == userId)
                    .Select(u => u.TeamId)
                    .FirstOrDefaultAsync();

                query = query.Where(t => t.Id == teamId);
            }

            var teams = await query
                .OrderBy(t => t.Name)
                .Select(TeamResponse.Projection)
                .ToListAsync();

            return Ok(teams);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetTeam(int id)
        {
            var team = await LoadTeamAsync(id);
            var userId = _access.GetUserId(User);

            if (team == null ||
                (!User.IsInRole(AppRoles.Admin) &&
                 team.ManagerId != userId &&
                 !team.Members.Any(m => m.Id == userId)))
            {
                return NotFound(new { message = "Team not found." });
            }

            return Ok(team);
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> CreateTeam(TeamRequest request)
        {
            var name = request.Name.Trim();

            if (await _db.Teams.AnyAsync(t => t.Name == name))
                return Conflict(new { message = "A team with this name already exists." });

            var managerError = await ValidateManagerAsync(request.ManagerId);
            if (managerError != null)
                return BadRequest(new { message = managerError });

            var team = new Team { Name = name, ManagerId = request.ManagerId };

            _db.Teams.Add(team);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTeam), new { id = team.Id }, await LoadTeamAsync(team.Id));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> UpdateTeam(int id, TeamRequest request)
        {
            var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
                return NotFound(new { message = "Team not found." });

            var name = request.Name.Trim();

            if (await _db.Teams.AnyAsync(t => t.Name == name && t.Id != id))
                return Conflict(new { message = "A team with this name already exists." });

            var managerError = await ValidateManagerAsync(request.ManagerId);
            if (managerError != null)
                return BadRequest(new { message = managerError });

            team.Name = name;
            team.ManagerId = request.ManagerId;
            await _db.SaveChangesAsync();

            return Ok(await LoadTeamAsync(id));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> DeleteTeam(int id)
        {
            var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
                return NotFound(new { message = "Team not found." });

            _db.Teams.Remove(team);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{id:int}/members")]
        [Authorize(Roles = AppRoles.AdminOrManager)]
        public async Task<IActionResult> AddMember(int id, AddMemberRequest request)
        {
            if (!await _db.Teams.AnyAsync(t => t.Id == id))
                return NotFound(new { message = "Team not found." });

            if (!await _access.CanManageTeamAsync(User, id))
                return Denied("You can only manage members of your own team.");

            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.Id == request.UserId && u.IsActive);

            if (user == null)
                return BadRequest(new { message = "User not found." });

            if (user.Role == AppRoles.Admin)
                return BadRequest(new { message = "Admins cannot be added to a team." });

            user.TeamId = id;
            await _db.SaveChangesAsync();

            return Ok(await LoadTeamAsync(id));
        }

        [HttpDelete("{id:int}/members/{userId:int}")]
        [Authorize(Roles = AppRoles.AdminOrManager)]
        public async Task<IActionResult> RemoveMember(int id, int userId)
        {
            if (!await _db.Teams.AnyAsync(t => t.Id == id))
                return NotFound(new { message = "Team not found." });

            if (!await _access.CanManageTeamAsync(User, id))
                return Denied("You can only manage members of your own team.");

            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.Id == userId && u.TeamId == id);

            if (user == null)
                return NotFound(new { message = "Member not found in this team." });

            user.TeamId = null;
            await _db.SaveChangesAsync();

            return Ok(await LoadTeamAsync(id));
        }
    }
}