using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<AppUser> Users { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<TaskComment> TaskComments { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppUser>(e =>
            {
                e.HasIndex(u => u.Email).IsUnique();

                e.HasOne(u => u.Team)
                    .WithMany(t => t.Members)
                    .HasForeignKey(u => u.TeamId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Team>(e =>
            {
                e.HasIndex(t => t.Name).IsUnique();

                e.HasOne(t => t.Manager)
                    .WithMany()
                    .HasForeignKey(t => t.ManagerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TaskItem>(e =>
            {
                e.HasOne(t => t.CreatedBy)
                    .WithMany()
                    .HasForeignKey(t => t.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(t => t.AssignedTo)
                    .WithMany()
                    .HasForeignKey(t => t.AssignedToId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(t => t.AssignedToId);
                e.HasIndex(t => t.Status);
                e.HasIndex(t => t.Deadline);
            });

            modelBuilder.Entity<TaskComment>(e =>
            {
                e.HasOne(c => c.TaskItem)
                    .WithMany(t => t.Comments)
                    .HasForeignKey(c => c.TaskItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Notification>(e =>
            {
                e.HasOne(n => n.User)
                    .WithMany()
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne<TaskItem>()
                    .WithMany()
                    .HasForeignKey(n => n.TaskItemId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(n => new { n.UserId, n.IsRead });
            });
        }
    }
}