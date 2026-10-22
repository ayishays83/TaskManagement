using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Api.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "To Do";

        [Required]
        [MaxLength(20)]
        public string Priority { get; set; } = "Medium";

        public DateTime? Deadline { get; set; }

        public int CreatedById { get; set; }
        public AppUser? CreatedBy { get; set; }

        public int AssignedToId { get; set; }
        public AppUser? AssignedTo { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
    }
}