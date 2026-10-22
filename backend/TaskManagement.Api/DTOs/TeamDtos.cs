using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.DTOs
{
    public class TeamRequest
    {
        [Required(ErrorMessage = "Team name is required.")]
        [StringLength(100, ErrorMessage = "Team name must be 100 characters or fewer.")]
        public string Name { get; set; } = "";

        public int? ManagerId { get; set; }
    }

    public class AddMemberRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "User is required.")]
        public int UserId { get; set; }
    }

    public class TeamMemberResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
    }

    public class TeamResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public List<TeamMemberResponse> Members { get; set; } = new();

        public static readonly Expression<Func<Team, TeamResponse>> Projection =
            t => new TeamResponse
            {
                Id = t.Id,
                Name = t.Name,
                ManagerId = t.ManagerId,
                ManagerName = t.Manager != null ? t.Manager.FullName : null,
                Members = t.Members
                    .OrderBy(m => m.FullName)
                    .Select(m => new TeamMemberResponse
                    {
                        Id = m.Id,
                        FullName = m.FullName,
                        Email = m.Email,
                        Role = m.Role
                    })
                    .ToList()
            };
    }
}