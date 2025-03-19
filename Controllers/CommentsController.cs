using DoAnWeb.Models;
using DoAnWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DoAnWeb.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class CommentsController : ControllerBase
    {
        private readonly DevCommunityContext _context;
        private readonly IQuestionRealTimeService _realTimeService;
        private readonly ILogger<CommentsController> _logger;

        public CommentsController(
            DevCommunityContext context,
            IQuestionRealTimeService realTimeService,
            ILogger<CommentsController> logger)
        {
            _context = context;
            _realTimeService = realTimeService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new comment on a question or answer
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CommentCreateModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Body))
                    return BadRequest(new { success = false, message = "Comment text cannot be empty" });

                // Get current user ID from claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                    return Unauthorized(new { success = false, message = "User not authenticated" });

                // Validate the target
                int targetId = model.TargetId;
                string targetType = model.TargetType;

                // Validate target type
                if (string.IsNullOrEmpty(targetType) || (targetType != "Question" && targetType != "Answer"))
                    return BadRequest(new { success = false, message = "Invalid target type" });

                // Create the comment
                var comment = new Comment
                {
                    Body = model.Body,
                    UserId = userId,
                    CreatedDate = DateTime.UtcNow,
                    TargetType = targetType,
                    TargetId = targetId
                };

                // Determine question ID and answer ID based on target type
                if (targetType == "Question")
                {
                    var question = await _context.Questions.FindAsync(targetId);
                    if (question == null)
                        return NotFound(new { success = false, message = "Question not found" });

                    comment.QuestionId = question.QuestionId;
                }
                else if (targetType == "Answer")
                {
                    var answer = await _context.Answers
                        .Include(a => a.Question)
                        .FirstOrDefaultAsync(a => a.AnswerId == targetId);

                    if (answer == null)
                        return NotFound(new { success = false, message = "Answer not found" });

                    comment.AnswerId = answer.AnswerId;
                    comment.QuestionId = answer.QuestionId;
                }

                // Save the comment
                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                // Get the comment with user information
                var commentWithUser = await _context.Comments
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.CommentId == comment.CommentId);

                // Notify clients about the new comment
                if (commentWithUser != null)
                {
                    await _realTimeService.NotifyNewComment(commentWithUser, targetType, targetId);
                }

                return Ok(new { 
                    success = true, 
                    commentId = comment.CommentId,
                    message = "Comment added successfully",
                    comment = new
                    {
                        commentId = comment.CommentId,
                        body = comment.Body,
                        userId = comment.UserId,
                        userName = comment.User?.Username ?? "Unknown user",
                        userAvatar = comment.User?.AvatarUrl,
                        createdDate = comment.CreatedDate,
                        targetType = targetType,
                        targetId = targetId,
                        questionId = comment.QuestionId,
                        answerId = comment.AnswerId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment");
                return StatusCode(500, new { success = false, message = "An error occurred while creating the comment" });
            }
        }
    }

    /// <summary>
    /// Model for creating a new comment
    /// </summary>
    public class CommentCreateModel
    {
        public string Body { get; set; }
        public string TargetType { get; set; } // "Question" or "Answer"
        public int TargetId { get; set; }
    }
} 