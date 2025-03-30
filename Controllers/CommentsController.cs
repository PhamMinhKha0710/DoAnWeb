using DoAnWeb.Models;
using DoAnWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using DoAnWeb.Hubs;

namespace DoAnWeb.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class CommentsController : Controller
    {
        private readonly DevCommunityContext _context;
        private readonly IQuestionRealTimeService _realTimeService;
        private readonly IHubContext<QuestionHub> _questionHubContext;
        private readonly ILogger<CommentsController> _logger;
        private readonly UserManager<User> _userManager;

        public CommentsController(
            DevCommunityContext context,
            IQuestionRealTimeService realTimeService,
            IHubContext<QuestionHub> questionHubContext,
            ILogger<CommentsController> logger,
            UserManager<User> userManager)
        {
            _context = context;
            _realTimeService = realTimeService;
            _questionHubContext = questionHubContext;
            _logger = logger;
            _userManager = userManager;
        }

        /// <summary>
        /// Creates a new comment on a question or answer, or a reply to another comment
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CommentCreateModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid comment data", errors = ModelState.Values.SelectMany(v => v.Errors) });
            }

            try
            {
                // Get current user ID from claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                // Find the user
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Unauthorized(new { success = false, message = "User not found" });
                }

                var isReply = model.ParentCommentId.HasValue;
                int? questionId = null;
                
                // Create the new comment
                var comment = new Comment
                {
                    Body = model.Body,
                    CreatedDate = DateTime.UtcNow,
                    UserId = userId,
                    TargetType = model.TargetType,
                    TargetId = model.TargetId,
                    ParentCommentId = model.ParentCommentId
                };

                // For replies, validate parent comment exists
                if (isReply)
                {
                    var parentComment = await _context.Comments
                        .FirstOrDefaultAsync(c => c.CommentId == model.ParentCommentId);
                    
                    if (parentComment == null)
                    {
                        return BadRequest(new { success = false, message = "Parent comment does not exist" });
                    }
                    
                    // If this is a reply, we need to get the Question ID to notify clients
                    if (parentComment.TargetType == "Question")
                    {
                        questionId = parentComment.TargetId;
                    }
                    else if (parentComment.TargetType == "Answer")
                    {
                        // Find the question for this answer
                        var answer = await _context.Answers.FindAsync(parentComment.TargetId);
                        if (answer != null)
                        {
                            questionId = answer.QuestionId;
                        }
                    }
                }
                else
                {
                    // Not a reply - determine question ID directly
                    if (model.TargetType == "Question")
                    {
                        questionId = model.TargetId;
                    }
                    else if (model.TargetType == "Answer")
                    {
                        // Find the question for this answer
                        var answer = await _context.Answers.FindAsync(model.TargetId);
                        if (answer != null)
                        {
                            questionId = answer.QuestionId;
                        }
                    }
                }

                // Add and save the comment
                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                // Prepare response data
                var commentResponse = new
                {
                    commentId = comment.CommentId,
                    body = comment.Body,
                    createdDate = comment.CreatedDate,
                    userId = comment.UserId,
                    userName = user.Username,
                    userDisplayName = user.DisplayName ?? user.Username,
                    userAvatar = user.AvatarUrl,
                    targetType = comment.TargetType,
                    targetId = comment.TargetId,
                    parentCommentId = comment.ParentCommentId,
                    questionId = questionId
                };

                // Notify clients through SignalR
                if (isReply)
                {
                    if (questionId.HasValue)
                    {
                        await _questionHubContext.Clients.Group($"question_{questionId}").SendAsync("ReceiveNewReply", commentResponse);
                        _logger.LogInformation($"Sent reply notification for comment {comment.CommentId} to group question_{questionId}");
                    }
                    else
                    {
                        _logger.LogWarning($"Could not determine question ID for reply notification. CommentId: {comment.CommentId}");
                    }
                }
                else
                {
                    if (questionId.HasValue)
                    {
                        await _questionHubContext.Clients.Group($"question_{questionId}").SendAsync("ReceiveNewComment", commentResponse);
                        _logger.LogInformation($"Sent comment notification for comment {comment.CommentId} to group question_{questionId}");
                    }
                    else
                    {
                        _logger.LogWarning($"Could not determine question ID for comment notification. CommentId: {comment.CommentId}");
                    }
                }

                return Json(new { success = true, comment = commentResponse });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment");
                return StatusCode(500, new { success = false, message = "An error occurred while creating the comment." });
            }
        }

        // GET: Comments
        public async Task<IActionResult> Index()
        {
            return View(await _context.Comments.ToListAsync());
        }

        // GET: Comments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.Comments
                .FirstOrDefaultAsync(m => m.CommentId == id);
            if (comment == null)
            {
                return NotFound();
            }

            return View(comment);
        }

        // GET: Comments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }

            // Check if current user is the author
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated" });
            }

            if (userId != comment.UserId)
            {
                return Forbid();
            }

            return View(comment);
        }

        // POST: Comments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("CommentId,Body")] Comment comment)
        {
            if (id != comment.CommentId)
            {
                return NotFound();
            }

            // Get the existing comment
            var existingComment = await _context.Comments.FindAsync(id);
            if (existingComment == null)
            {
                return NotFound();
            }

            // Check if current user is the author
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated" });
            }

            if (userId != existingComment.UserId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update only allowed fields
                    existingComment.Body = comment.Body;
                    existingComment.EditedDate = DateTime.UtcNow;
                    existingComment.IsEdited = true;

                    _context.Update(existingComment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CommentExists(comment.CommentId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(comment);
        }

        // POST: Comments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }

            // Check if current user is the author or has permission
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated" });
            }

            bool isAdmin = User.IsInRole("Admin");
            
            if (userId != comment.UserId && !isAdmin)
            {
                return Forbid();
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CommentExists(int id)
        {
            return _context.Comments.Any(e => e.CommentId == id);
        }
    }

    /// <summary>
    /// Model for creating a new comment
    /// </summary>
    public class CommentCreateModel
    {
        [Required]
        public string Body { get; set; }
        
        [Required]
        public string TargetType { get; set; }
        
        [Required]
        public int TargetId { get; set; }
        
        public int? ParentCommentId { get; set; }
    }
} 