using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.DTOs
{
    public class ChangeRoleRequest
    {
        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = "";
    }

    public class UserResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public int? TeamId { get; set; }
        public string? TeamName { get; set; }

        public static readonly Expression<Func<AppUser, UserResponse>> Projection =
            u => new UserResponse
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role,
                TeamId = u.TeamId,
                TeamName = u.Team != null ? u.Team.Name : null
            };
    }

    public class NotificationResponse
    {
        public int Id { get; set; }
        public int? TaskItemId { get; set; }
        public string Type { get; set; } = "";
        public string Message { get; set; } = "";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}