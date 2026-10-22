using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TaskManagement.Api.Services;
using Xunit;

namespace TaskManagement.Tests;

public class NotificationServiceTests
{
    private static NotificationService CreateService(Api.Data.AppDbContext db) =>
        new(db, NullLogger<NotificationService>.Instance);

    [Fact]
    public async Task TaskAssigned_NotifiesTheAssignee()
    {
        var db = await TestHelpers.SeedAsync();
        var service = CreateService(db);
        var task = await db.Tasks.FindAsync(1);
        var assignee = await db.Users.FindAsync(3);
        var actor = await db.Users.FindAsync(2);

        await service.TaskAssignedAsync(task!, assignee!, actor!);

        var notification = await db.Notifications.SingleAsync();
        Assert.Equal(3, notification.UserId);
        Assert.Equal(task!.Id, notification.TaskItemId);
        Assert.Equal("TaskAssigned", notification.Type);
        Assert.Contains("User 2", notification.Message);
        Assert.Contains("Task 1", notification.Message);
    }

    [Fact]
    public async Task TaskAssigned_DoesNotNotifyWhenAssigningToYourself()
    {
        var db = await TestHelpers.SeedAsync();
        var service = CreateService(db);
        var task = await db.Tasks.FindAsync(5);
        var manager = await db.Users.FindAsync(2);

        await service.TaskAssignedAsync(task!, manager!, manager!);

        Assert.Empty(await db.Notifications.ToListAsync());
    }

    [Fact]
    public async Task StatusChanged_NotifiesAssigneeAndCreator_WhenAnotherUserChangesIt()
    {
        var db = await TestHelpers.SeedAsync();
        var service = CreateService(db);

        // t1: created by manager (2), assigned to user (3). Admin (1) changes the status.
        var task = await db.Tasks.FindAsync(1);
        task!.Status = TaskStatuses.InProgress;
        var actor = await db.Users.FindAsync(1);

        await service.TaskStatusChangedAsync(task, TaskStatuses.ToDo, actor!);

        var notifications = await db.Notifications.ToListAsync();
        Assert.Equal(2, notifications.Count);
        Assert.Contains(notifications, n => n.UserId == 2);
        Assert.Contains(notifications, n => n.UserId == 3);
        Assert.All(notifications, n => Assert.Equal("TaskStatusChanged", n.Type));
        Assert.All(notifications, n => Assert.Contains("from To Do to In Progress", n.Message));
    }

    [Fact]
    public async Task StatusChanged_DoesNotNotifyTheUserWhoMadeTheChange()
    {
        var db = await TestHelpers.SeedAsync();
        var service = CreateService(db);

        // t1: the assignee (3) changes the status, so only the creator (2) is notified.
        var task = await db.Tasks.FindAsync(1);
        task!.Status = TaskStatuses.Done;
        var actor = await db.Users.FindAsync(3);

        await service.TaskStatusChangedAsync(task, TaskStatuses.ToDo, actor!);

        var notification = await db.Notifications.SingleAsync();
        Assert.Equal(2, notification.UserId);
    }

    [Fact]
    public async Task StatusChanged_SendsNothing_WhenActorIsBothCreatorAndAssignee()
    {
        var db = await TestHelpers.SeedAsync();
        db.Tasks.Add(TestHelpers.MakeTask(10, createdById: 3, assignedToId: 3));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var task = await db.Tasks.FindAsync(10);
        task!.Status = TaskStatuses.Done;
        var actor = await db.Users.FindAsync(3);

        await service.TaskStatusChangedAsync(task, TaskStatuses.ToDo, actor!);

        Assert.Empty(await db.Notifications.ToListAsync());
    }
}