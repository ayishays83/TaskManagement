using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Data;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Services
{
    public interface ITaskAccessService
    {
        int GetUserId(ClaimsPrincipal user);
        IQueryable<TaskItem> VisibleTasks(ClaimsPrincipal user);
        Task<bool> CanAssignToAsync(ClaimsPrincipal user, AppUser assignee);
        Task<bool> CanManageTeamAsync(ClaimsPrincipal user, int teamId);
    }

    public class TaskAccessService : ITaskAccessService
    {
        private readonly AppDbContext _db;

        public TaskAccessService(AppDbContext db)
        {
            _db = db;
        }

        public int GetUserId(ClaimsPrincipal user) =>
            int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public IQueryable<TaskItem> VisibleTasks(ClaimsPrincipal user)
        {
            var userId = GetUserId(user);

            if (user.IsInRole(AppRoles.Admin))
                return _db.Tasks;

            if (user.IsInRole(AppRoles.Manager))
            {
                var memberIds = _db.Users
                    .Where(u => u.TeamId != null &&
                                _db.Teams.Any(t => t.Id == u.TeamId && t.ManagerId == userId))
                    .Select(u => u.Id);

                return _db.Tasks.Where(t =>
                    t.CreatedById == userId ||
                    t.AssignedToId == userId ||
                    memberIds.Contains(t.AssignedToId));
            }

            return _db.Tasks.Where(t => t.AssignedToId == userId);
        }

        public async Task<bool> CanAssignToAsync(ClaimsPrincipal user, AppUser assignee)
        {
            if (!assignee.IsActive) return false;

            if (user.IsInRole(AppRoles.Admin))
                return assignee.Role == AppRoles.Manager || assignee.Role == AppRoles.User;

            if (user.IsInRole(AppRoles.Manager))
            {
                if (assignee.TeamId == null) return false;
                var managerId = GetUserId(user);

                return await _db.Teams.AnyAsync(t =>
                    t.Id == assignee.TeamId && t.ManagerId == managerId);
            }

            return false;
        }

        public async Task<bool> CanManageTeamAsync(ClaimsPrincipal user, int teamId)
        {
            if (user.IsInRole(AppRoles.Admin)) return true;
            if (!user.IsInRole(AppRoles.Manager)) return false;

            var managerId = GetUserId(user);
            return await _db.Teams.AnyAsync(t => t.Id == teamId && t.ManagerId == managerId);
        }
    }
}