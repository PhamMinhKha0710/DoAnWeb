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

        public QuestionHub(ILogger<QuestionHub> logger)
        {
            _logger = logger;
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
    }
} 