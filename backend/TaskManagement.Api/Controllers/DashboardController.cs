using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Data;
using TaskManagement.Api.Services;

namespace TaskManagement.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ITaskAccessService _access;

        public DashboardController(AppDbContext db, ITaskAccessService access)
        {
            _db = db;
            _access = access;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var tasks = _access.VisibleTasks(User);
            var today = DateTime.UtcNow.Date;

            var byStatus = await tasks
                .GroupBy(t => t.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            var byPriority = await tasks
                .GroupBy(t => t.Priority)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            var overdue = await tasks.CountAsync(t =>
                t.Deadline != null &&
                t.Deadline < today &&
                t.Status != TaskStatuses.Done);

            return Ok(new
            {
                TotalTasks = byStatus.Values.Sum(),
                ToDo = byStatus.GetValueOrDefault(TaskStatuses.ToDo),
                InProgress = byStatus.GetValueOrDefault(TaskStatuses.InProgress),
                Done = byStatus.GetValueOrDefault(TaskStatuses.Done),
                LowPriority = byPriority.GetValueOrDefault(TaskPriorities.Low),
                MediumPriority = byPriority.GetValueOrDefault(TaskPriorities.Medium),
                HighPriority = byPriority.GetValueOrDefault(TaskPriorities.High),
                Overdue = overdue
            });
        }

        // Task status overview per user (Admin: everyone, Manager: own team).
        [HttpGet("per-user")]
        [Authorize(Roles = AppRoles.AdminOrManager)]
        public async Task<IActionResult> GetPerUser()
        {
            var tasks = _access.VisibleTasks(User);
            var today = DateTime.UtcNow.Date;

            var rows = await tasks
                .GroupBy(t => t.AssignedToId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Total = g.Count(),
                    ToDo = g.Count(t => t.Status == TaskStatuses.ToDo),
                    InProgress = g.Count(t => t.Status == TaskStatuses.InProgress),
                    Done = g.Count(t => t.Status == TaskStatuses.Done),
                    Overdue = g.Count(t =>
                        t.Deadline != null &&
                        t.Deadline < today &&
                        t.Status != TaskStatuses.Done)
                })
                .ToListAsync();

            var ids = rows.Select(r => r.UserId).ToList();

            var names = await _db.Users
                .Where(u => ids.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName);

            var result = rows
                .Select(r => new
                {
                    r.UserId,
                    UserName = names.GetValueOrDefault(r.UserId, "Unknown"),
                    r.Total,
                    r.ToDo,
                    r.InProgress,
                    r.Done,
                    r.Overdue
                })
                .OrderBy(r => r.UserName)
                .ToList();

            return Ok(result);
        }
    }
}