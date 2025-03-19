using DoAnWeb.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace DoAnWeb.Hubs
{
    /// <summary>
    /// SignalR hub for handling real-time question and answer interactions
    /// </summary>
    public class QuestionHub : Hub
    {
        private readonly ILogger<QuestionHub> _logger;
        private readonly DevCommunityContext _context;

        public QuestionHub(ILogger<QuestionHub> logger, DevCommunityContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// When a client connects, join them to the general questions group
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            try
            {
                // Connect to general questions group to see all new questions
                await Groups.AddToGroupAsync(Context.ConnectionId, "AllQuestions");
                _logger.LogInformation($"Client {Context.ConnectionId} connected to QuestionHub");
                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in QuestionHub.OnConnectedAsync");
                await base.OnConnectedAsync();
            }
        }

        /// <summary>
        /// Join a specific question's group to receive updates about answers
        /// </summary>
        public async Task JoinQuestionGroup(int questionId)
        {
            if (questionId <= 0)
                return;

            await Groups.AddToGroupAsync(Context.ConnectionId, $"Question-{questionId}");
            _logger.LogInformation($"Client {Context.ConnectionId} joined Question-{questionId} group");
        }

        /// <summary>
        /// Leave a specific question's group
        /// </summary>
        public async Task LeaveQuestionGroup(int questionId)
        {
            if (questionId <= 0)
                return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Question-{questionId}");
        }

        /// <summary>
        /// Send a new question to all connected clients
        /// </summary>
        public async Task SendNewQuestion(Question question)
        {
            try
            {
                _logger.LogInformation($"Broadcasting new question: {question.QuestionId} - {question.Title}");
                await Clients.Group("AllQuestions").SendAsync("ReceiveNewQuestion", question);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error broadcasting new question {question.QuestionId}");
            }
        }

        /// <summary>
        /// Send a new answer to all clients viewing the related question
        /// </summary>
        public async Task SendNewAnswer(Answer answer)
        {
            try
            {
                if (answer?.QuestionId == null || answer.QuestionId <= 0)
                {
                    _logger.LogWarning("Attempted to broadcast answer without valid QuestionId");
                    return;
                }

                _logger.LogInformation($"Broadcasting new answer: {answer.AnswerId} for question {answer.QuestionId}");
                
                // Send to specific question group and to all questions group
                await Clients.Group($"Question-{answer.QuestionId}").SendAsync("ReceiveNewAnswer", answer);
                await Clients.Group("AllQuestions").SendAsync("ReceiveNewAnswerCount", answer.QuestionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error broadcasting new answer {answer.AnswerId} for question {answer.QuestionId}");
            }
        }
        
        /// <summary>
        /// Send a new comment to all clients viewing the related question
        /// </summary>
        public async Task SendNewComment(Comment comment, string targetType, int targetId)
        {
            try
            {
                _logger.LogInformation($"Broadcasting new comment: {comment.CommentId} for {targetType} {targetId}");
                
                // Send to all clients viewing the related question
                int questionId = 0;
                if (targetType.Equals("Question", StringComparison.OrdinalIgnoreCase))
                {
                    questionId = targetId;
                }
                else if (targetType.Equals("Answer", StringComparison.OrdinalIgnoreCase))
                {
                    // Tìm questionId từ cơ sở dữ liệu 
                    var answer = await _context.Answers.FindAsync(targetId);
                    if (answer != null && answer.QuestionId.HasValue)
                    {
                        questionId = answer.QuestionId.Value;
                    }
                }
                
                if (questionId > 0)
                {
                    await Clients.Group($"Question-{questionId}").SendAsync("ReceiveNewComment", comment, targetType, targetId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error broadcasting new comment {comment.CommentId}");
            }
        }
    }
} 