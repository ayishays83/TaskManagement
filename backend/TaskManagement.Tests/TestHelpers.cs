using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Data;
using TaskManagement.Api.Models;
using TaskManagement.Api.Services;

namespace TaskManagement.Tests;

public static class TestHelpers
{
    public static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    public static ClaimsPrincipal Principal(int id, string role)
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Role, role)
            },
            authenticationType: "Test");

        return new ClaimsPrincipal(identity);
    }

    public static AppUser MakeUser(int id, string role, int? teamId = null, bool isActive = true) =>
        new()
        {
            Id = id,
            FullName = $"User {id}",
            Email = $"user{id}@test.com",
            PasswordHash = "x",
            Role = role,
            TeamId = teamId,
            IsActive = isActive
        };

    public static TaskItem MakeTask(int id, int createdById, int assignedToId, string status = TaskStatuses.ToDo) =>
        new()
        {
            Id = id,
            Title = $"Task {id}",
            Status = status,
            CreatedById = createdById,
            AssignedToId = assignedToId
        };

    /// <summary>
    /// Users: 1 Admin, 2 Manager (team 1), 3 User (team 1), 4 User (no team),
    ///        5 Manager (team 2), 6 User (team 2), 7 inactive User (team 1).
    /// Tasks: t1 (2 -> 3), t2 (1 -> 3), t3 (5 -> 6), t4 (1 -> 4), t5 (1 -> 2).
    /// </summary>
    public static async Task<AppDbContext> SeedAsync()
    {
        var db = CreateDb();

        db.Users.AddRange(
            MakeUser(1, AppRoles.Admin),
            MakeUser(2, AppRoles.Manager),
            MakeUser(3, AppRoles.User, teamId: 1),
            MakeUser(4, AppRoles.User),
            MakeUser(5, AppRoles.Manager),
            MakeUser(6, AppRoles.User, teamId: 2),
            MakeUser(7, AppRoles.User, teamId: 1, isActive: false));

        db.Teams.AddRange(
            new Team { Id = 1, Name = "Team One", ManagerId = 2 },
            new Team { Id = 2, Name = "Team Two", ManagerId = 5 });

        db.Tasks.AddRange(
            MakeTask(1, createdById: 2, assignedToId: 3),
            MakeTask(2, createdById: 1, assignedToId: 3),
            MakeTask(3, createdById: 5, assignedToId: 6),
            MakeTask(4, createdById: 1, assignedToId: 4),
            MakeTask(5, createdById: 1, assignedToId: 2));

        await db.SaveChangesAsync();
        return db;
    }
}