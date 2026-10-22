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
    public class TaskController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ITaskAccessService _access;
        private readonly INotificationService _notifications;

        public TaskController(
            AppDbContext db,
            ITaskAccessService access,
            INotificationService notifications)
        {
            _db = db;
            _access = access;
            _notifications = notifications;
        }

        [HttpGet]
        public async Task<IActionResult> GetTasks(
            string? status,
            string? priority,
            DateTime? deadlineFrom,
            DateTime? deadlineTo,
            int? assignedToId)
        {
            var query = _access.VisibleTasks(User);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(t => t.Status == status);

            if (!string.IsNullOrWhiteSpace(priority))
                query = query.Where(t => t.Priority == priority);

            if (assignedToId.HasValue)
                query = query.Where(t => t.AssignedToId == assignedToId.Value);

            if (deadlineFrom.HasValue)
            {
                var from = deadlineFrom.Value.Date;
                query = query.Where(t => t.Deadline != null && t.Deadline >= from);
            }

            if (deadlineTo.HasValue)
            {
                var to = deadlineTo.Value.Date.AddDays(1);
                query = query.Where(t => t.Deadline != null && t.Deadline < to);
            }

            var tasks = await query
                .OrderBy(t => t.Deadline == null)
                .ThenBy(t => t.Deadline)
                .ThenByDescending(t => t.Id)
                .Select(TaskResponse.Projection)
                .ToListAsync();

            return Ok(tasks);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetTask(int id)
        {
            var task = await _access.VisibleTasks(User)
                .Where(t => t.Id == id)
                .Select(TaskResponse.Projection)
                .FirstOrDefaultAsync();

            if (task == null)
                return NotFound(new { message = "Task not found." });

            return Ok(task);
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.AdminOrManager)]
        public async Task<IActionResult> CreateTask(CreateTaskRequest request)
        {
            if (!TaskPriorities.All.Contains(request.Priority))
                return BadRequest(new { message = "Invalid priority." });

            var assignee = await _db.Users.FirstOrDefaultAsync(u =>
                u.Id == request.AssignedToId && u.IsActive);

            if (assignee == null)
                return BadRequest(new { message = "Assignee not found." });

            if (!await _access.CanAssignToAsync(User, assignee))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "You are not allowed to assign tasks to this user."
                });
            }

            var actorId = _access.GetUserId(User);
            var actor = await _db.Users.FirstAsync(u => u.Id == actorId);

            var task = new TaskItem
            {
                Title = request.Title.Trim(),
                Description = request.Description?.Trim() ?? "",
                Priority = request.Priority,
                Deadline = request.Deadline,
                AssignedToId = assignee.Id,
                CreatedById = actorId,
                Status = TaskStatuses.ToDo
            };

            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();

            await _notifications.TaskAssignedAsync(task, assignee, actor);

            var response = await _db.Tasks
                .Where(t => t.Id == task.Id)
                .Select(TaskResponse.Projection)
                .FirstAsync();

            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, response);
        }

        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateStatusRequest request)
        {
            if (!TaskStatuses.All.Contains(request.Status))
                return BadRequest(new { message = "Invalid status." });

            var task = await _access.VisibleTasks(User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound(new { message = "Task not found." });

            var oldStatus = task.Status;

            if (oldStatus != request.Status)
            {
                task.Status = request.Status;
                await _db.SaveChangesAsync();

                var actorId = _access.GetUserId(User);
                var actor = await _db.Users.FirstAsync(u => u.Id == actorId);

                await _notifications.TaskStatusChangedAsync(task, oldStatus, actor);
            }

            var response = await _db.Tasks
                .Where(t => t.Id == id)
                .Select(TaskResponse.Projection)
                .FirstAsync();

            return Ok(response);
        }
    }
}