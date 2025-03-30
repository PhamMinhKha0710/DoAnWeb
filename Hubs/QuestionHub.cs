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
        /// Join a specific question's group to receive updates about answers and comments
        /// </summary>
        public async Task JoinQuestionGroup(int questionId)
        {
            if (questionId <= 0)
                return;

            // Update the group name format to match what's used in the controller
            string groupName = $"question_{questionId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation($"Client {Context.ConnectionId} joined {groupName} group");
        }

        /// <summary>
        /// Leave a specific question's group
        /// </summary>
        public async Task LeaveQuestionGroup(int questionId)
        {
            if (questionId <= 0)
                return;

            // Update the group name format to match what's used in the controller
            string groupName = $"question_{questionId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation($"Client {Context.ConnectionId} left {groupName} group");
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
                
                // Update the group name format to match what's used in the controller
                string groupName = $"question_{answer.QuestionId}";
                await Clients.Group(groupName).SendAsync("ReceiveNewAnswer", answer);
                await Clients.Group("AllQuestions").SendAsync("ReceiveNewAnswerCount", answer.QuestionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error broadcasting answer for question {answer?.QuestionId}");
            }
        }
        
        /// <summary>
        /// Send a new comment to all clients viewing the related question
        /// </summary>
        public async Task SendNewComment(Comment comment, int questionId)
        {
            try
            {
                if (questionId <= 0)
                {
                    _logger.LogWarning("Attempted to broadcast comment without valid QuestionId");
                    return;
                }

                _logger.LogInformation($"Broadcasting new comment: {comment.CommentId} for question {questionId}");
                
                // Use the correct group name format
                string groupName = $"question_{questionId}";
                
                // If it's a reply (has parent comment), use ReceiveNewReply
                if (comment.ParentCommentId.HasValue)
                {
                    await Clients.Group(groupName).SendAsync("ReceiveNewReply", comment);
                    _logger.LogInformation($"Sent reply notification for comment {comment.CommentId} to group {groupName}");
                }
                else
                {
                    await Clients.Group(groupName).SendAsync("ReceiveNewComment", comment);
                    _logger.LogInformation($"Sent comment notification for comment {comment.CommentId} to group {groupName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error broadcasting comment for question {questionId}");
            }
        }
    }
} 