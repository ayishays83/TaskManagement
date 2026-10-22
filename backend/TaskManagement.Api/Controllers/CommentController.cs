using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Data;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Models;
using TaskManagement.Api.Services;

namespace TaskManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CommentController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ITaskAccessService _access;

        public CommentController(AppDbContext db, ITaskAccessService access)
        {
            _db = db;
            _access = access;
        }

        [HttpGet("{taskId:int}")]
        public async Task<IActionResult> GetComments(int taskId)
        {
            if (!await _access.VisibleTasks(User).AnyAsync(t => t.Id == taskId))
                return NotFound(new { message = "Task not found." });

            var comments = await _db.TaskComments
                .Where(c => c.TaskItemId == taskId)
                .OrderBy(c => c.CreatedAt)
                .Select(CommentResponse.Projection)
                .ToListAsync();

            return Ok(comments);
        }

        [HttpPost("{taskId:int}")]
        public async Task<IActionResult> AddComment(int taskId, CreateCommentRequest request)
        {
            if (!await _access.VisibleTasks(User).AnyAsync(t => t.Id == taskId))
                return NotFound(new { message = "Task not found." });

            var comment = new TaskComment
            {
                TaskItemId = taskId,
                UserId = _access.GetUserId(User),
                Content = request.Content.Trim()
            };

            _db.TaskComments.Add(comment);
            await _db.SaveChangesAsync();

            var response = await _db.TaskComments
                .Where(c => c.Id == comment.Id)
                .Select(CommentResponse.Projection)
                .FirstAsync();

            return Ok(response);
        }
    }
}