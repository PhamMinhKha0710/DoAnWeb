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
        private readonly ReputationService _reputationService;

        public VoteController(
            IQuestionService questionService,
            IAnswerService answerService,
            INotificationService notificationService,
            IHubContext<NotificationHub> notificationHubContext,
            ReputationService reputationService)
        {
            _questionService = questionService;
            _answerService = answerService;
            _notificationService = notificationService;
            _notificationHubContext = notificationHubContext;
            _reputationService = reputationService;
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

                    // Tracking values for reputation updates
                    bool reputationChanged = false;
                    bool isNewUpvote = false;
                    bool isRemovedUpvote = false;
                    bool isNewDownvote = false;
                    bool isRemovedDownvote = false;
                    bool isChangedToUpvote = false;
                    bool isChangedToDownvote = false;

                    // Handle vote
                    if (existingVote != null)
                    {
                        if (!isUpvote.HasValue)
                        {
                            // Remove vote
                            _questionService.RemoveVote(existingVote.VoteId);
                            
                            // Update reputation flags
                            if (existingVote.IsUpvote)
                            {
                                isRemovedUpvote = true;
                                reputationChanged = true;
                            }
                            else
                            {
                                isRemovedDownvote = true;
                                reputationChanged = true;
                            }
                        }
                        else if (existingVote.IsUpvote != isUpvote.Value)
                        {
                            // Change vote direction
                            bool oldIsUpvote = existingVote.IsUpvote;
                            existingVote.IsUpvote = isUpvote.Value;
                            existingVote.VoteDate = DateTime.UtcNow;
                            _questionService.UpdateVote(existingVote);
                            
                            // Set user vote for response
                            userVote = isUpvote.Value ? 1 : -1;
                            
                            // Update reputation flags
                            if (isUpvote.Value)
                            {
                                isChangedToUpvote = true;
                                reputationChanged = true;
                            }
                            else
                            {
                                isChangedToDownvote = true;
                                reputationChanged = true;
                            }
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
                        
                        // Update reputation flags
                        if (isUpvote.Value)
                        {
                            isNewUpvote = true;
                            reputationChanged = true;
                        }
                        else
                        {
                            isNewDownvote = true;
                            reputationChanged = true;
                        }
                    }

                    // Get updated score
                    question = _questionService.GetQuestionById(request.ItemId);
                    newScore = question.Score ?? 0;

                    // Update reputation if needed
                    if (reputationChanged && question.UserId != userId)
                    {
                        if (isNewUpvote || isChangedToUpvote)
                        {
                            await _reputationService.UpdateReputationForActionAsync(
                                question.UserId.Value, 
                                ReputationActionType.QuestionUpvoted, 
                                question.QuestionId);
                        }
                        else if (isNewDownvote || isChangedToDownvote)
                        {
                            await _reputationService.UpdateReputationForActionAsync(
                                question.UserId.Value, 
                                ReputationActionType.QuestionDownvoted, 
                                question.QuestionId);
                        }
                        else if (isRemovedUpvote)
                        {
                            // Removing an upvote reverses the +10
                            await _reputationService.UpdateReputationAsync(
                                question.UserId.Value,
                                -10,
                                $"Removed upvote on question (ID: {question.QuestionId})");
                        }
                        else if (isRemovedDownvote)
                        {
                            // Removing a downvote reverses the -2
                            await _reputationService.UpdateReputationAsync(
                                question.UserId.Value,
                                2,
                                $"Removed downvote on question (ID: {question.QuestionId})");
                        }
                    }

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
                                questionId = question.QuestionId,
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
                    var answer = await _answerService.GetAnswerByIdAsync(request.ItemId);
                    if (answer == null)
                    {
                        return NotFound(new { success = false, message = "Answer not found" });
                    }

                    // Get existing vote
                    var existingVote = await _questionService.GetUserVoteOnAnswerAsync(userId, request.ItemId);

                    // Tracking values for reputation updates
                    bool reputationChanged = false;
                    bool isNewUpvote = false;
                    bool isRemovedUpvote = false;
                    bool isNewDownvote = false;
                    bool isRemovedDownvote = false;
                    bool isChangedToUpvote = false;
                    bool isChangedToDownvote = false;

                    // Handle vote
                    if (existingVote != null)
                    {
                        if (!isUpvote.HasValue)
                        {
                            // Remove vote
                            await _questionService.RemoveVoteAsync(existingVote.VoteId);
                            
                            // Update reputation flags
                            if (existingVote.IsUpvote)
                            {
                                isRemovedUpvote = true;
                                reputationChanged = true;
                            }
                            else
                            {
                                isRemovedDownvote = true;
                                reputationChanged = true;
                            }
                        }
                        else if (existingVote.IsUpvote != isUpvote.Value)
                        {
                            // Change vote direction
                            bool oldIsUpvote = existingVote.IsUpvote;
                            existingVote.IsUpvote = isUpvote.Value;
                            existingVote.VoteDate = DateTime.UtcNow;
                            await _questionService.UpdateVoteAsync(existingVote);
                            
                            // Set user vote for response
                            userVote = isUpvote.Value ? 1 : -1;
                            
                            // Update reputation flags
                            if (isUpvote.Value)
                            {
                                isChangedToUpvote = true;
                                reputationChanged = true;
                            }
                            else
                            {
                                isChangedToDownvote = true;
                                reputationChanged = true;
                            }
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

                        await _questionService.AddVoteAsync(vote);
                        
                        // Set user vote for response
                        userVote = isUpvote.Value ? 1 : -1;
                        
                        // Update reputation flags
                        if (isUpvote.Value)
                        {
                            isNewUpvote = true;
                            reputationChanged = true;
                        }
                        else
                        {
                            isNewDownvote = true;
                            reputationChanged = true;
                        }
                    }

                    // Get updated score
                    answer = await _answerService.GetAnswerByIdAsync(request.ItemId);
                    newScore = answer.Score ?? 0;

                    // Update reputation if needed
                    if (reputationChanged && answer.UserId != userId)
                    {
                        if (isNewUpvote || isChangedToUpvote)
                        {
                            await _reputationService.UpdateReputationForActionAsync(
                                answer.UserId.Value, 
                                ReputationActionType.AnswerUpvoted, 
                                answer.AnswerId);
                        }
                        else if (isNewDownvote || isChangedToDownvote)
                        {
                            await _reputationService.UpdateReputationForActionAsync(
                                answer.UserId.Value, 
                                ReputationActionType.AnswerDownvoted, 
                                answer.AnswerId);
                        }
                        else if (isRemovedUpvote)
                        {
                            // Removing an upvote reverses the +10
                            await _reputationService.UpdateReputationAsync(
                                answer.UserId.Value,
                                -10,
                                $"Removed upvote on answer (ID: {answer.AnswerId})");
                        }
                        else if (isRemovedDownvote)
                        {
                            // Removing a downvote reverses the -2
                            await _reputationService.UpdateReputationAsync(
                                answer.UserId.Value,
                                2,
                                $"Removed downvote on answer (ID: {answer.AnswerId})");
                        }
                    }

                    // Send real-time notification through SignalR
                    if (isUpvote.HasValue && isUpvote.Value && answer.UserId != userId)
                    {
                        // Get current user display name
                        var currentUserName = User.Identity?.Name ?? "Someone";
                        var displayName = User.FindFirst("DisplayName")?.Value ?? currentUserName;

                        // Get question title (shortened if needed)
                        var questionTitle = answer.Question?.Title;
                        if (questionTitle != null && questionTitle.Length > 50)
                        {
                            questionTitle = questionTitle.Substring(0, 47) + "...";
                        }

                        // Make sure we have non-nullable Ids
                        int safeAnswerId = answer.AnswerId;
                        int safeQuestionId = answer.Question?.QuestionId ?? 0;
                        
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