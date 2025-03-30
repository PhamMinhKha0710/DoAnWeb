using DoAnWeb.Hubs;
using DoAnWeb.Models;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace DoAnWeb.Services
{
    public interface IQuestionRealTimeService
    {
        Task NotifyNewQuestion(Question question);
        Task NotifyNewAnswer(Answer answer);
        Task NotifyQuestionUpdated(Question question);
        Task NotifyNewComment(Comment comment, string targetType, int targetId);
    }

    public class QuestionRealTimeService : IQuestionRealTimeService
    {
        private readonly IHubContext<QuestionHub> _questionHubContext;
        private readonly ILogger<QuestionRealTimeService> _logger;
        private readonly DevCommunityContext _context;

        public QuestionRealTimeService(
            IHubContext<QuestionHub> questionHubContext,
            ILogger<QuestionRealTimeService> logger,
            DevCommunityContext context)
        {
            _questionHubContext = questionHubContext;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Notify all connected clients about a new question
        /// </summary>
        public async Task NotifyNewQuestion(Question question)
        {
            try
            {
                if (question == null || question.QuestionId <= 0)
                {
                    _logger.LogWarning("Attempted to notify about null or invalid question");
                    return;
                }

                // Create simplified object with only needed data to reduce payload size
                var questionData = new
                {
                    question.QuestionId,
                    question.Title,
                    question.Body,
                    question.UserId,
                    UserName = question.User?.Username ?? "Unknown user",
                    UserAvatar = question.User?.AvatarUrl,
                    question.CreatedDate,
                    TagCount = question.QuestionTags?.Count ?? 0,
                    Tags = question.QuestionTags?.Select(qt => new { qt.Tag.TagId, qt.Tag.TagName }).ToList(),
                    AnswerCount = question.Answers?.Count ?? 0,
                    question.ViewCount,
                    question.Score
                };

                // Send to all connected clients
                await _questionHubContext.Clients.Group("AllQuestions")
                    .SendAsync("ReceiveNewQuestion", questionData);

                _logger.LogInformation($"Notified clients about new question: {question.QuestionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error notifying about new question: {question.QuestionId}");
            }
        }

        /// <summary>
        /// Notify clients viewing a specific question about a new answer
        /// </summary>
        public async Task NotifyNewAnswer(Answer answer)
        {
            try
            {
                if (answer == null || !answer.QuestionId.HasValue || answer.QuestionId <= 0)
                {
                    _logger.LogWarning("Attempted to notify about null or invalid answer");
                    return;
                }

                // Create simplified object with only needed data
                var answerData = new
                {
                    answer.AnswerId,
                    answer.QuestionId,
                    answer.Body,
                    answer.UserId,
                    UserName = answer.User?.Username ?? "Unknown user",
                    UserAvatar = answer.User?.AvatarUrl,
                    answer.CreatedDate,
                    answer.UpdatedDate,
                    answer.IsAccepted,
                    answer.Score
                };

                // Send to clients viewing this question
                await _questionHubContext.Clients.Group($"Question-{answer.QuestionId}")
                    .SendAsync("ReceiveNewAnswer", answerData);

                // Also update answer count for clients viewing the questions list
                await _questionHubContext.Clients.Group("AllQuestions")
                    .SendAsync("ReceiveAnswerCountUpdate", new { QuestionId = answer.QuestionId, NewCount = 1 });

                _logger.LogInformation($"Notified clients about new answer: {answer.AnswerId} for question: {answer.QuestionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error notifying about new answer: {answer.AnswerId}");
            }
        }

        /// <summary>
        /// Notify clients about a new comment or reply
        /// </summary>
        public async Task NotifyNewComment(Comment comment, string targetType, int targetId)
        {
            try
            {
                if (comment == null || comment.CommentId <= 0)
                {
                    _logger.LogWarning("Attempted to notify about null or invalid comment");
                    return;
                }

                // Load related data if needed
                if (comment.User == null)
                {
                    var user = await _context.Users.FindAsync(comment.UserId);
                    comment.User = user;
                }

                // Determine question ID for notification
                int? questionId = null;
                
                if (targetType == "Question")
                {
                    questionId = targetId;
                }
                else if (targetType == "Answer")
                {
                    var answer = await _context.Answers.FindAsync(targetId);
                    questionId = answer?.QuestionId;
                }
                else if (targetType == "Comment" && comment.ParentCommentId.HasValue)
                {
                    // For replies to comments, we need to find the parent comment's target
                    var parentComment = await _context.Comments.FindAsync(comment.ParentCommentId);
                    if (parentComment != null)
                    {
                        if (parentComment.TargetType == "Question")
                        {
                            questionId = parentComment.TargetId;
                        }
                        else if (parentComment.TargetType == "Answer")
                        {
                            var answer = await _context.Answers.FindAsync(parentComment.TargetId);
                            questionId = answer?.QuestionId;
                        }
                    }
                }

                if (!questionId.HasValue)
                {
                    _logger.LogWarning($"Could not determine question ID for comment notification. CommentId: {comment.CommentId}");
                    return;
                }

                // Create simplified object with only needed data
                var commentData = new
                {
                    comment.CommentId,
                    comment.Body,
                    comment.UserId,
                    UserName = comment.User?.Username ?? "Unknown user",
                    UserDisplayName = comment.User?.DisplayName ?? "Unknown user",
                    UserAvatar = comment.User?.AvatarUrl,
                    comment.CreatedDate,
                    comment.TargetType,
                    comment.TargetId,
                    QuestionId = questionId,
                    comment.ParentCommentId,
                    IsReply = comment.ParentCommentId.HasValue
                };

                string eventName = comment.ParentCommentId.HasValue 
                    ? "ReceiveNewReply" 
                    : "ReceiveNewComment";

                // Send notification to the correct group with updated format
                string groupName = $"question_{questionId}";
                await _questionHubContext.Clients.Group(groupName).SendAsync(eventName, commentData);

                _logger.LogInformation($"Notified clients about new {(comment.ParentCommentId.HasValue ? "reply" : "comment")}: {comment.CommentId} for question: {questionId} via {groupName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error notifying about new comment: {comment.CommentId}");
            }
        }

        /// <summary>
        /// Notify clients about question updates (edits, status changes, etc.)
        /// </summary>
        public async Task NotifyQuestionUpdated(Question question)
        {
            try
            {
                if (question == null || question.QuestionId <= 0)
                {
                    _logger.LogWarning("Attempted to notify about null or invalid question update");
                    return;
                }

                // Create simplified update object
                var updateData = new
                {
                    question.QuestionId,
                    question.Title,
                    Body = question.Body,
                    IsResolved = question.Status == "Resolved",
                    LastUpdated = DateTime.UtcNow,
                    question.UpdatedDate,
                    question.Score,
                    question.ViewCount,
                    Tags = question.QuestionTags?.Select(qt => new { qt.Tag.TagId, qt.Tag.TagName }).ToList()
                };

                // Send to clients viewing this question
                await _questionHubContext.Clients.Group($"Question-{question.QuestionId}")
                    .SendAsync("ReceiveQuestionUpdate", updateData);

                // Also update in the questions list
                await _questionHubContext.Clients.Group("AllQuestions")
                    .SendAsync("ReceiveQuestionUpdate", updateData);

                _logger.LogInformation($"Notified clients about question update: {question.QuestionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error notifying about question update: {question.QuestionId}");
            }
        }
    }
} 