using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using DoAnWeb.Models;
using DoAnWeb.Services;
using DoAnWeb.Hubs;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DoAnWeb.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class VoteController : ControllerBase
    {
        private readonly IQuestionService _questionService;
        private readonly IAnswerService _answerService;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotificationHub> _notificationHubContext;

        public VoteController(
            IQuestionService questionService,
            IAnswerService answerService,
            INotificationService notificationService,
            IHubContext<NotificationHub> notificationHubContext)
        {
            _questionService = questionService;
            _answerService = answerService;
            _notificationService = notificationService;
            _notificationHubContext = notificationHubContext;
        }

        /// <summary>
        /// Handles AJAX vote requests for questions and answers
        /// </summary>
        [HttpPost("Cast")]
        [Authorize]
        public async Task<IActionResult> Cast([FromBody] VoteRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { success = false, message = "Invalid request" });
            }

            // Get logged-in user ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated" });
            }

            // Validate request parameters
            if (request.ItemId <= 0 || string.IsNullOrEmpty(request.ItemType) || string.IsNullOrEmpty(request.VoteType))
            {
                return BadRequest(new { success = false, message = "Invalid vote parameters" });
            }

            // Process vote
            try
            {
                int newScore = 0;
                int userVote = 0; // 0 = no vote, 1 = upvote, -1 = downvote

                // Determine vote direction
                bool? isUpvote = null;
                if (request.VoteType == "up")
                    isUpvote = true;
                else if (request.VoteType == "down")
                    isUpvote = false;
                // Otherwise, it's a "remove" vote

                if (request.ItemType.Equals("question", StringComparison.OrdinalIgnoreCase))
                {
                    // Get question
                    var question = _questionService.GetQuestionById(request.ItemId);
                    if (question == null)
                    {
                        return NotFound(new { success = false, message = "Question not found" });
                    }

                    // Get existing vote
                    var existingVote = _questionService.GetUserVoteOnQuestion(userId, request.ItemId);

                    // Handle vote
                    if (existingVote != null)
                    {
                        if (!isUpvote.HasValue)
                        {
                            // Remove vote
                            _questionService.RemoveVote(existingVote.VoteId);
                        }
                        else if (existingVote.IsUpvote != isUpvote.Value)
                        {
                            // Change vote direction
                            existingVote.IsUpvote = isUpvote.Value;
                            existingVote.VoteDate = DateTime.UtcNow;
                            _questionService.UpdateVote(existingVote);
                            
                            // Set user vote for response
                            userVote = isUpvote.Value ? 1 : -1;
                        }
                        else
                        {
                            // User is voting the same way again, respond with current state
                            return Ok(new { 
                                success = true, 
                                message = "Vote unchanged", 
                                newScore = question.Score ?? 0,
                                userVote = existingVote.IsUpvote ? 1 : -1
                            });
                        }
                    }
                    else if (isUpvote.HasValue)
                    {
                        // New vote
                        var vote = new Vote
                        {
                            UserId = userId,
                            TargetId = request.ItemId,
                            TargetType = "Question",
                            IsUpvote = isUpvote.Value,
                            VoteDate = DateTime.UtcNow,
                            VoteValue = isUpvote.Value ? 1 : -1
                        };

                        _questionService.AddVote(vote);
                        
                        // Set user vote for response
                        userVote = isUpvote.Value ? 1 : -1;
                    }

                    // Get updated score
                    question = _questionService.GetQuestionById(request.ItemId);
                    newScore = question.Score ?? 0;

                    // Send real-time notification through SignalR
                    if (isUpvote.HasValue && isUpvote.Value && question.UserId != userId)
                    {
                        // Get current user display name
                        var currentUserName = User.Identity?.Name ?? "Someone";
                        var displayName = User.FindFirst("DisplayName")?.Value ?? currentUserName;

                        // Prepare question title (shortened if needed)
                        var questionTitle = question.Title;
                        if (questionTitle.Length > 50)
                        {
                            questionTitle = questionTitle.Substring(0, 47) + "...";
                        }

                        // Make sure we have a non-nullable QuestionId
                        int safeQuestionId = question.QuestionId;

                        // Send real-time notification to question owner
                        await _notificationHubContext.Clients
                            .Group($"user-{question.UserId}")
                            .SendAsync("ReceiveNotification", new {
                                id = Guid.NewGuid().ToString(), 
                                type = "Vote", 
                                title = "New Upvote",
                                message = $"{displayName} upvoted your question: \"{questionTitle}\"",
                                questionId = safeQuestionId,
                                score = newScore,
                                userId = userId,
                                url = $"/Questions/Details/{safeQuestionId}", 
                                createdDate = DateTime.UtcNow.ToString("o") 
                            });

                        // Also create database notification - ensure non-nullable int
                        await _notificationService.NotifyVoteAsync("Question", safeQuestionId, userId);
                    }
                }
                else if (request.ItemType.Equals("answer", StringComparison.OrdinalIgnoreCase))
                {
                    // Get answer
                    var answer = _answerService.GetAnswerById(request.ItemId);
                    if (answer == null)
                    {
                        return NotFound(new { success = false, message = "Answer not found" });
                    }

                    // Get existing vote
                    var existingVote = _questionService.GetUserVoteOnAnswer(userId, request.ItemId);

                    // Handle vote
                    if (existingVote != null)
                    {
                        if (!isUpvote.HasValue)
                        {
                            // Remove vote
                            _questionService.RemoveVote(existingVote.VoteId);
                        }
                        else if (existingVote.IsUpvote != isUpvote.Value)
                        {
                            // Change vote direction
                            existingVote.IsUpvote = isUpvote.Value;
                            existingVote.VoteDate = DateTime.UtcNow;
                            _questionService.UpdateVote(existingVote);
                            
                            // Set user vote for response
                            userVote = isUpvote.Value ? 1 : -1;
                        }
                        else
                        {
                            // User is voting the same way again, respond with current state
                            return Ok(new { 
                                success = true, 
                                message = "Vote unchanged", 
                                newScore = answer.Score ?? 0,
                                userVote = existingVote.IsUpvote ? 1 : -1
                            });
                        }
                    }
                    else if (isUpvote.HasValue)
                    {
                        // New vote
                        var vote = new Vote
                        {
                            UserId = userId,
                            TargetId = request.ItemId,
                            TargetType = "Answer",
                            IsUpvote = isUpvote.Value,
                            VoteDate = DateTime.UtcNow,
                            VoteValue = isUpvote.Value ? 1 : -1
                        };

                        _questionService.AddVote(vote);
                        
                        // Set user vote for response
                        userVote = isUpvote.Value ? 1 : -1;
                    }

                    // Get updated score
                    answer = _answerService.GetAnswerById(request.ItemId);
                    newScore = answer.Score ?? 0;

                    // Send real-time notification through SignalR
                    if (isUpvote.HasValue && isUpvote.Value && answer.UserId != userId)
                    {
                        // Get current user display name
                        var currentUserName = User.Identity?.Name ?? "Someone";
                        var displayName = User.FindFirst("DisplayName")?.Value ?? currentUserName;
                        
                        // Get associated question for context
                        int questionIdValue = answer.QuestionId.HasValue ? answer.QuestionId.Value : 0;
                        var associatedQuestion = _questionService.GetQuestionById(questionIdValue);
                        var questionTitle = associatedQuestion?.Title ?? "a question";
                        if (questionTitle.Length > 50)
                        {
                            questionTitle = questionTitle.Substring(0, 47) + "...";
                        }

                        // Make sure we have non-nullable IDs
                        int safeAnswerId = answer.AnswerId;
                        int safeQuestionId = answer.QuestionId.HasValue ? answer.QuestionId.Value : 0;

                        // Send real-time notification to answer owner
                        await _notificationHubContext.Clients
                            .Group($"user-{answer.UserId}")
                            .SendAsync("ReceiveNotification", new {
                                id = Guid.NewGuid().ToString(),
                                type = "Vote",
                                title = "New Upvote",
                                message = $"{displayName} upvoted your answer to the question: \"{questionTitle}\"",
                                answerId = safeAnswerId,
                                questionId = safeQuestionId,
                                score = newScore,
                                userId = userId,
                                url = $"/Questions/Details/{safeQuestionId}#answer-{safeAnswerId}",
                                createdDate = DateTime.UtcNow.ToString("o")
                            });

                        // Also create database notification - ensure non-nullable int
                        await _notificationService.NotifyVoteAsync("Answer", safeAnswerId, userId);
                    }
                }
                else
                {
                    return BadRequest(new { success = false, message = "Invalid item type" });
                }

                return Ok(new { success = true, newScore, userVote });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred processing your vote", error = ex.Message });
            }
        }
    }

    public class VoteRequest
    {
        public int ItemId { get; set; }
        public string ItemType { get; set; }
        public string VoteType { get; set; }
    }
} 