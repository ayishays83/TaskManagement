using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Services;
using Xunit;

namespace TaskManagement.Tests;

public class TaskAccessServiceTests
{
    private static async Task<TaskAccessService> CreateServiceAsync()
    {
        var db = await TestHelpers.SeedAsync();
        return new TaskAccessService(db);
    }

    private static async Task<int[]> VisibleIdsAsync(TaskAccessService service, int userId, string role)
    {
        var tasks = await service.VisibleTasks(TestHelpers.Principal(userId, role)).ToListAsync();
        return tasks.Select(t => t.Id).OrderBy(id => id).ToArray();
    }

    [Fact]
    public async Task GetUserId_ReadsTheNameIdentifierClaim()
    {
        var service = await CreateServiceAsync();

        var id = service.GetUserId(TestHelpers.Principal(42, AppRoles.User));

        Assert.Equal(42, id);
    }

    // ---------- VisibleTasks ----------

    [Fact]
    public async Task Admin_SeesAllTasks()
    {
        var service = await CreateServiceAsync();

        var ids = await VisibleIdsAsync(service, 1, AppRoles.Admin);

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, ids);
    }

    [Fact]
    public async Task Manager_SeesCreatedAssignedAndTeamMemberTasks()
    {
        var service = await CreateServiceAsync();

        // t1 created by manager, t2 assigned to a team member, t5 assigned to the manager.
        var ids = await VisibleIdsAsync(service, 2, AppRoles.Manager);

        Assert.Equal(new[] { 1, 2, 5 }, ids);
    }

    [Fact]
    public async Task Manager_DoesNotSeeOtherTeamsTasks()
    {
        var service = await CreateServiceAsync();

        var ids = await VisibleIdsAsync(service, 5, AppRoles.Manager);

        Assert.Equal(new[] { 3 }, ids);
    }

    [Fact]
    public async Task User_SeesOnlyTasksAssignedToThem()
    {
        var service = await CreateServiceAsync();

        var ids = await VisibleIdsAsync(service, 3, AppRoles.User);

        Assert.Equal(new[] { 1, 2 }, ids);
    }

    [Fact]
    public async Task User_WithNoTasks_SeesNothing()
    {
        var service = await CreateServiceAsync();

        var ids = await VisibleIdsAsync(service, 6, AppRoles.User);

        Assert.Equal(new[] { 3 }, ids); // user 6 has exactly t3
        var none = await VisibleIdsAsync(service, 99, AppRoles.User);
        Assert.Empty(none);
    }

    // ---------- CanAssignTo ----------

    [Theory]
    [InlineData(2, true)]  // Manager
    [InlineData(3, true)]  // User
    [InlineData(1, false)] // Admin cannot be assigned to
    public async Task Admin_CanAssignOnlyToManagersAndUsers(int assigneeId, bool expected)
    {
        var db = await TestHelpers.SeedAsync();
        var service = new TaskAccessService(db);
        var assignee = await db.Users.FindAsync(assigneeId);

        var result = await service.CanAssignToAsync(TestHelpers.Principal(1, AppRoles.Admin), assignee!);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Admin_CannotAssignToInactiveUser()
    {
        var db = await TestHelpers.SeedAsync();
        var service = new TaskAccessService(db);
        var inactive = await db.Users.FindAsync(7);

        var result = await service.CanAssignToAsync(TestHelpers.Principal(1, AppRoles.Admin), inactive!);

        Assert.False(result);
    }

    [Fact]
    public async Task Manager_CanAssignToMemberOfOwnTeam()
    {
        var db = await TestHelpers.SeedAsync();
        var service = new TaskAccessService(db);
        var member = await db.Users.FindAsync(3);

        var result = await service.CanAssignToAsync(TestHelpers.Principal(2, AppRoles.Manager), member!);

        Assert.True(result);
    }

    [Fact]
    public async Task Manager_CannotAssignToMemberOfAnotherTeam()
    {
        var db = await TestHelpers.SeedAsync();
        var service = new TaskAccessService(db);
        var otherTeamMember = await db.Users.FindAsync(6);

        var result = await service.CanAssignToAsync(TestHelpers.Principal(2, AppRoles.Manager), otherTeamMember!);

        Assert.False(result);
    }

    [Fact]
    public async Task Manager_CannotAssignToUserWithoutTeam()
    {
        var db = await TestHelpers.SeedAsync();
        var service = new TaskAccessService(db);
        var noTeam = await db.Users.FindAsync(4);

        var result = await service.CanAssignToAsync(TestHelpers.Principal(2, AppRoles.Manager), noTeam!);

        Assert.False(result);
    }

    [Fact]
    public async Task Manager_CannotAssignToAdmin()
    {
        var db = await TestHelpers.SeedAsync();
        var service = new TaskAccessService(db);
        var admin = await db.Users.FindAsync(1);

        var result = await service.CanAssignToAsync(TestHelpers.Principal(2, AppRoles.Manager), admin!);

        Assert.False(result);
    }

    [Fact]
    public async Task User_CannotAssignToAnyone()
    {
        var db = await TestHelpers.SeedAsync();
        var service = new TaskAccessService(db);
        var teammate = await db.Users.FindAsync(3);

        var result = await service.CanAssignToAsync(TestHelpers.Principal(3, AppRoles.User), teammate!);

        Assert.False(result);
    }

    // ---------- CanManageTeam ----------

    [Fact]
    public async Task Admin_CanManageAnyTeam()
    {
        var service = await CreateServiceAsync();

        Assert.True(await service.CanManageTeamAsync(TestHelpers.Principal(1, AppRoles.Admin), 1));
        Assert.True(await service.CanManageTeamAsync(TestHelpers.Principal(1, AppRoles.Admin), 2));
    }

    [Fact]
    public async Task Manager_CanManageOnlyOwnTeam()
    {
        var service = await CreateServiceAsync();
        var manager = TestHelpers.Principal(2, AppRoles.Manager);

        Assert.True(await service.CanManageTeamAsync(manager, 1));
        Assert.False(await service.CanManageTeamAsync(manager, 2));
    }

    [Fact]
    public async Task User_CannotManageAnyTeam()
    {
        var service = await CreateServiceAsync();

        Assert.False(await service.CanManageTeamAsync(TestHelpers.Principal(3, AppRoles.User), 1));
    }
}