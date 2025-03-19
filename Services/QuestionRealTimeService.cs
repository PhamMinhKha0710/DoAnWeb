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
        /// Notify clients about a new comment on a question or answer
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

                int questionId = 0;
                
                // Determine which question this comment belongs to
                if (targetType.Equals("Question", StringComparison.OrdinalIgnoreCase))
                {
                    questionId = targetId;
                }
                else if (targetType.Equals("Answer", StringComparison.OrdinalIgnoreCase))
                {
                    // Tìm questionId từ đối tượng Answer
                    var answer = await _context.Answers.FindAsync(targetId);
                    if (answer != null && answer.QuestionId.HasValue)
                    {
                        questionId = answer.QuestionId.Value;
                    }
                }
                
                if (questionId <= 0)
                {
                    _logger.LogWarning($"Could not determine question ID for comment {comment.CommentId}");
                    return;
                }

                // Create comment data to send
                var commentData = new
                {
                    comment.CommentId,
                    comment.Body,
                    comment.UserId,
                    UserName = comment.User?.Username ?? "Unknown user",
                    UserAvatar = comment.User?.AvatarUrl,
                    comment.CreatedDate,
                    TargetType = targetType,
                    TargetId = targetId,
                    QuestionId = questionId
                };

                // Send to clients viewing this question
                await _questionHubContext.Clients.Group($"Question-{questionId}")
                    .SendAsync("ReceiveNewComment", commentData);

                _logger.LogInformation($"Notified clients about new comment: {comment.CommentId} for {targetType} {targetId}");
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