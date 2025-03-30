using DoAnWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
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
                        
                        // Add to reputation group
                        await Groups.AddToGroupAsync(Context.ConnectionId, $"reputation-{userIdInt}");

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
                groupName.StartsWith("answer-") || groupName.StartsWith("user-") ||
                groupName.StartsWith("reputation-"))
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
        
        /// <summary>
        /// Gửi thông báo về thay đổi điểm reputation cho người dùng
        /// </summary>
        /// <param name="userId">ID của người dùng</param>
        /// <param name="newReputation">Điểm reputation mới</param>
        /// <param name="reason">Lý do thay đổi điểm</param>
        /// <returns>Task</returns>
        public async Task SendReputationUpdate(int userId, int newReputation, string reason)
        {
            try
            {
                // Kiểm tra quyền - chỉ admin hoặc system mới có thể gọi phương thức này
                if (!IsAdminOrSystem())
                {
                    _logger.LogWarning($"Unauthorized attempt to call SendReputationUpdate by {Context.User?.Identity?.Name}");
                    return;
                }
                
                // Gửi thông báo đến nhóm reputation của người dùng
                await Clients.Group($"reputation-{userId}").SendAsync("ReputationChanged", userId, newReputation, reason);
                
                _logger.LogInformation($"Reputation update sent for user {userId}: new value = {newReputation}, reason = {reason}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending reputation update for user {userId}");
            }
        }
        
        /// <summary>
        /// Check if the current user is an admin or system
        /// </summary>
        private bool IsAdminOrSystem()
        {
            // System claim là một claim đặc biệt cho các service backgrounds
            var isSystem = Context.User.HasClaim(c => c.Type == "System" && c.Value == "true");
            
            // Admin check
            var isAdmin = Context.User.IsInRole("Admin");
            
            return isSystem || isAdmin;
        }
    }
}