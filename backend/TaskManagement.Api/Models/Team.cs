using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Api.Models
{
    public class Team
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public int? ManagerId { get; set; }
        public AppUser? Manager { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<AppUser> Members { get; set; } = new List<AppUser>();
    }
}