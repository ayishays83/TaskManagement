using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.DTOs
{
    public class CreateCommentRequest
    {
        [Required(ErrorMessage = "Comment is required.")]
        [StringLength(1000, ErrorMessage = "Comment must be 1000 characters or fewer.")]
        public string Content { get; set; } = "";
    }

    public class CommentResponse
    {
        public int Id { get; set; }
        public int TaskItemId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; }

        public static readonly Expression<Func<TaskComment, CommentResponse>> Projection =
            c => new CommentResponse
            {
                Id = c.Id,
                TaskItemId = c.TaskItemId,
                UserId = c.UserId,
                UserName = c.User!.FullName,
                Content = c.Content,
                CreatedAt = c.CreatedAt
            };
    }
}