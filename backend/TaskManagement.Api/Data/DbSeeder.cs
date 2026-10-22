using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Models;
using TaskManagement.Api.Services;

namespace TaskManagement.Api.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            if (await db.Users.AnyAsync(u => u.Email == "admin@taskflow.com"))
                return;

            var admin = new AppUser
            {
                FullName = "Admin User",
                Email = "admin@taskflow.com",
                PasswordHash = PasswordHelper.Hash("Admin@123"),
                Role = "Admin"
            };

            var manager = new AppUser
            {
                FullName = "Maya Manager",
                Email = "manager@taskflow.com",
                PasswordHash = PasswordHelper.Hash("Manager@123"),
                Role = "Manager"
            };

            var user1 = new AppUser
            {
                FullName = "Uma User",
                Email = "user1@taskflow.com",
                PasswordHash = PasswordHelper.Hash("User@1234"),
                Role = "User"
            };

            var user2 = new AppUser
            {
                FullName = "Ravi User",
                Email = "user2@taskflow.com",
                PasswordHash = PasswordHelper.Hash("User@1234"),
                Role = "User"
            };

            db.Users.AddRange(admin, manager, user1, user2);
            await db.SaveChangesAsync();

            var team = new Team { Name = "Engineering", ManagerId = manager.Id };
            db.Teams.Add(team);
            await db.SaveChangesAsync();

            manager.TeamId = team.Id;
            user1.TeamId = team.Id;
            user2.TeamId = team.Id;

            var today = DateTime.UtcNow.Date;

            db.Tasks.AddRange(
                new TaskItem
                {
                    Title = "Design database schema",
                    Description = "Define tables, relations and indexes.",
                    Status = "Done",
                    Priority = "High",
                    Deadline = today.AddDays(-2),
                    CreatedById = manager.Id,
                    AssignedToId = user1.Id
                },
                new TaskItem
                {
                    Title = "Build login page",
                    Description = "JWT login form with validation.",
                    Status = "In Progress",
                    Priority = "Medium",
                    Deadline = today.AddDays(3),
                    CreatedById = manager.Id,
                    AssignedToId = user1.Id
                },
                new TaskItem
                {
                    Title = "Write API documentation",
                    Description = "Swagger and Postman collection.",
                    Status = "To Do",
                    Priority = "Low",
                    Deadline = today.AddDays(7),
                    CreatedById = manager.Id,
                    AssignedToId = user2.Id
                },
                new TaskItem
                {
                    Title = "Fix overdue report bug",
                    Description = "Report shows wrong totals.",
                    Status = "To Do",
                    Priority = "High",
                    Deadline = today.AddDays(-1),
                    CreatedById = manager.Id,
                    AssignedToId = user2.Id
                });

            await db.SaveChangesAsync();
        }
    }
}