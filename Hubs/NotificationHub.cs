using DoAnWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DoAnWeb.Hubs
{
    /// <summary>
    /// SignalR hub for handling real-time notifications
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly DevCommunityContext _context;
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(DevCommunityContext context, ILogger<NotificationHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Connect user to their personal notification channel on connection
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            try
            {
                // Get current user ID from claims
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    // Connect user to their personal group
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
                    
                    if (int.TryParse(userId, out int userIdInt))
                    {
                        // Find questions the user is interested in
                        var watchedQuestions = _context.Questions
                            .Where(q => q.UserId == userIdInt)
                            .Select(q => q.QuestionId)
                            .ToList();

                        // Add to answer notification groups for their questions
                        foreach (var questionId in watchedQuestions)
                        {
                            await Groups.AddToGroupAsync(Context.ConnectionId, $"question-{questionId}");
                            // Also add to the upvote notification group for each question
                            await Groups.AddToGroupAsync(Context.ConnectionId, $"question-upvote-{questionId}");
                        }

                        // Find answers the user has posted (to get upvote notifications)
                        var userAnswers = _context.Answers
                            .Where(a => a.UserId == userIdInt)
                            .Select(a => a.AnswerId)
                            .ToList();

                        // Add to upvote notification groups for their answers
                        foreach (var answerId in userAnswers)
                        {
                            await Groups.AddToGroupAsync(Context.ConnectionId, $"answer-upvote-{answerId}");
                        }

                        // Find tags user is watching
                        var watchedTags = _context.UserWatchedTags
                            .Where(t => t.UserId == userIdInt)
                            .Select(t => t.TagId)
                            .ToList();

                        // Add to tag notification groups
                        foreach (var tagId in watchedTags)
                        {
                            await Groups.AddToGroupAsync(Context.ConnectionId, $"tag-{tagId}");
                        }

                        _logger.LogInformation($"User {userId} connected to NotificationHub and joined {watchedQuestions.Count} question groups, {userAnswers.Count} answer groups, and {watchedTags.Count} tag groups");
                    }
                }
                
                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NotificationHub.OnConnectedAsync");
                await base.OnConnectedAsync();
            }
        }

        /// <summary>
        /// Join a specific notification group
        /// </summary>
        public async Task JoinGroup(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                return;

            // Security check - only allow joining groups the user should have access to
            if (groupName.StartsWith("question-") || groupName.StartsWith("tag-") || 
                groupName.StartsWith("answer-") || groupName.StartsWith("user-"))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                _logger.LogInformation($"User {Context.User?.Identity?.Name} joined group {groupName}");
            }
        }

        /// <summary>
        /// Leave a specific notification group
        /// </summary>
        public async Task LeaveGroup(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        /// <summary>
        /// Mark a notification as read
        /// </summary>
        public async Task MarkAsRead(int notificationId)
        {
            try
            {
                // Get user ID from claims
                var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return;

                // Find notification
                var notification = await _context.Notifications.FindAsync(notificationId);
                
                // Check ownership
                if (notification != null && notification.UserId == userId)
                {
                    // Mark as read
                    notification.IsRead = true;
                    await _context.SaveChangesAsync();
                    
                    // Confirm to client
                    await Clients.Caller.SendAsync("NotificationMarkedAsRead", notificationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking notification {notificationId} as read");
            }
        }

        /// <summary>
        /// Mark all notifications as read
        /// </summary>
        public async Task MarkAllAsRead()
        {
            try
            {
                // Get user ID from claims
                var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return;

                // Find all unread notifications for this user
                var notifications = _context.Notifications
                    .Where(n => n.UserId == userId && n.IsRead == false)
                    .ToList();

                // Mark all as read
                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                }

                await _context.SaveChangesAsync();
                
                // Confirm to client
                await Clients.Caller.SendAsync("AllNotificationsMarkedAsRead");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
            }
        }
    }
}