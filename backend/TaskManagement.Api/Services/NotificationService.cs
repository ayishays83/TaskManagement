using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Data;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Services
{
    public interface INotificationService
    {
        Task TaskAssignedAsync(TaskItem task, AppUser assignee, AppUser actor);
        Task TaskStatusChangedAsync(TaskItem task, string oldStatus, AppUser actor);
    }

    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(AppDbContext db, ILogger<NotificationService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task TaskAssignedAsync(TaskItem task, AppUser assignee, AppUser actor)
        {
            if (assignee.Id == actor.Id) return;

            Queue(
                assignee,
                task.Id,
                "TaskAssigned",
                "New task assigned",
                $"{actor.FullName} assigned you the task \"{task.Title}\".");

            await _db.SaveChangesAsync();
        }

        public async Task TaskStatusChangedAsync(TaskItem task, string oldStatus, AppUser actor)
        {
            var recipientIds = new[] { task.AssignedToId, task.CreatedById }
                .Distinct()
                .Where(id => id != actor.Id)
                .ToList();

            if (recipientIds.Count == 0) return;

            var recipients = await _db.Users
                .Where(u => recipientIds.Contains(u.Id))
                .ToListAsync();

            foreach (var recipient in recipients)
            {
                Queue(
                    recipient,
                    task.Id,
                    "TaskStatusChanged",
                    "Task status updated",
                    $"{actor.FullName} moved \"{task.Title}\" from {oldStatus} to {task.Status}.");
            }

            await _db.SaveChangesAsync();
        }

        private void Queue(AppUser recipient, int taskId, string type, string subject, string message)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = recipient.Id,
                TaskItemId = taskId,
                Type = type,
                Message = message
            });

            _logger.LogInformation(
                "[MOCK EMAIL] To: {Email} | Subject: {Subject} | Body: {Body}",
                recipient.Email, subject, message);
        }
    }
}