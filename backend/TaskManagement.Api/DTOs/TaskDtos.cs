using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.DTOs
{
    public class CreateTaskRequest
    {
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(150, ErrorMessage = "Title must be 150 characters or fewer.")]
        public string Title { get; set; } = "";

        [StringLength(1000, ErrorMessage = "Description must be 1000 characters or fewer.")]
        public string Description { get; set; } = "";

        public string Priority { get; set; } = "Medium";

        public DateTime? Deadline { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Assignee is required.")]
        public int AssignedToId { get; set; }
    }

    public class UpdateStatusRequest
    {
        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; } = "";
    }

    public class TaskResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "";
        public string Priority { get; set; } = "";
        public DateTime? Deadline { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedById { get; set; }
        public string CreatedByName { get; set; } = "";
        public int AssignedToId { get; set; }
        public string AssignedToName { get; set; } = "";

        public static readonly Expression<Func<TaskItem, TaskResponse>> Projection =
            t => new TaskResponse
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                Priority = t.Priority,
                Deadline = t.Deadline,
                CreatedAt = t.CreatedAt,
                CreatedById = t.CreatedById,
                CreatedByName = t.CreatedBy!.FullName,
                AssignedToId = t.AssignedToId,
                AssignedToName = t.AssignedTo!.FullName
            };
    }
}