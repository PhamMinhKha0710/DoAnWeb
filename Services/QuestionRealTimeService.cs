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
    }

    public class QuestionRealTimeService : IQuestionRealTimeService
    {
        private readonly IHubContext<QuestionHub> _questionHubContext;
        private readonly ILogger<QuestionRealTimeService> _logger;

        public QuestionRealTimeService(
            IHubContext<QuestionHub> questionHubContext,
            ILogger<QuestionRealTimeService> logger)
        {
            _questionHubContext = questionHubContext;
            _logger = logger;
        }

        /// <summary>
        /// Notify all connected clients about a new question
        /// </summary>
        public async Task NotifyNewQuestion(Question question)
        {
            try
            {
                // Create simplified object with only needed data to reduce payload size
                var questionData = new
                {
                    question.QuestionId,
                    question.Title,
                    question.UserId,
                    Username = question.User?.Username ?? "Unknown user",
                    question.CreatedDate,
                    TagCount = question.QuestionTags?.Count ?? 0
                };

                // Send to all connected clients
                await _questionHubContext.Clients.Group("AllQuestions")
                    .SendAsync("NewQuestionPosted", questionData);

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
                if (answer == null || answer.QuestionId <= 0)
                {
                    _logger.LogWarning("Attempted to notify about null or invalid answer");
                    return;
                }

                // Create simplified object with only needed data
                var answerData = new
                {
                    answer.AnswerId,
                    answer.QuestionId,
                    Content = answer.Body,
                    answer.UserId,
                    Username = answer.User?.Username ?? "Unknown user",
                    answer.CreatedDate,
                    answer.IsAccepted
                };

                // Send to clients viewing this question
                await _questionHubContext.Clients.Group($"Question-{answer.QuestionId}")
                    .SendAsync("NewAnswerPosted", answerData);

                _logger.LogInformation($"Notified clients about new answer: {answer.AnswerId} for question: {answer.QuestionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error notifying about new answer: {answer.AnswerId}");
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
                    Content = question.Body,
                    IsResolved = question.Status == "Resolved",
                    LastUpdated = DateTime.UtcNow
                };

                // Send to clients viewing this question
                await _questionHubContext.Clients.Group($"Question-{question.QuestionId}")
                    .SendAsync("QuestionUpdated", updateData);

                _logger.LogInformation($"Notified clients about question update: {question.QuestionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error notifying about question update: {question.QuestionId}");
            }
        }
    }
} 