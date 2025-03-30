using DoAnWeb.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnWeb.Services
{
    /// <summary>
    /// Service for handling notifications with security and queue processing
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly DevCommunityContext _context;
        private readonly NotificationBackgroundService _backgroundService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            DevCommunityContext context,
            NotificationBackgroundService backgroundService,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _backgroundService = backgroundService;
            _logger = logger;
        }

        /// <summary>
        /// Get all notifications for a user
        /// </summary>
        public async Task<List<Notification>> GetUserNotificationsAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get unread notifications for a user
        /// </summary>
        public async Task<List<Notification>> GetUnreadNotificationsAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && n.IsRead == false)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get unread notifications count for a user
        /// </summary>
        public async Task<int> GetUnreadNotificationCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && n.IsRead == false);
        }

        /// <summary>
        /// Mark a notification as read with ownership check
        /// </summary>
        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            
            // Security check - only allow users to mark their own notifications as read
            if (notification == null || notification.UserId != userId)
                return false;

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Mark all notifications as read for a user
        /// </summary>
        public async Task<int> MarkAllAsReadAsync(int userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.IsRead == false)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return unreadNotifications.Count;
        }

        /// <summary>
        /// Create a notification and process it through the queue
        /// </summary>
        public async Task<Notification> CreateNotificationAsync(Notification notification, List<string> recipientGroups = null)
        {
            try
            {
                // Queue the notification for background processing
                _backgroundService.QueueNotification(new NotificationQueueItem
                {
                    Notification = notification,
                    SendRealTime = true,
                    RecipientGroups = recipientGroups ?? new List<string>()
                });

                return notification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                throw;
            }
        }

        /// <summary>
        /// Create a notification when a new answer is posted
        /// </summary>
        public async Task NotifyNewAnswerAsync(int questionId, int answerId, int userId)
        {
            try
            {
                // Get question details
                var question = await _context.Questions
                    .Include(q => q.User)
                    .FirstOrDefaultAsync(q => q.QuestionId == questionId);

                if (question == null)
                    return;

                // Find answer details
                var answer = await _context.Answers.FindAsync(answerId);
                if (answer == null)
                    return;

                // Don't notify yourself
                if (question.UserId == userId)
                    return;

                // Create notification for question owner
                var notification = new Notification
                {
                    UserId = question.UserId,
                    Title = "New Answer",
                    Message = $"Your question '{question.Title}' has received a new answer.",
                    Url = $"/Questions/Details/{questionId}#answer-{answerId}",
                    IsRead = false,
                    CreatedDate = DateTime.Now,
                    NotificationType = NotificationTypeConstants.Answer,
                    RelatedEntityId = answerId
                };

                // Create group list for real-time delivery
                var recipientGroups = new List<string> { $"user-{question.UserId}", $"question-{questionId}" };

                // Queue notification with real-time delivery to groups
                await CreateNotificationAsync(notification, recipientGroups);

                _logger.LogInformation($"New answer notification created for question {questionId}, sent to user {question.UserId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating new answer notification for question {questionId}, answer {answerId}");
            }
        }

        /// <summary>
        /// Create a notification when a new comment is posted
        /// </summary>
        public async Task NotifyNewCommentAsync(string targetType, int targetId, int commentId, int userId)
        {
            try
            {
                int? recipientId = null;
                string title = string.Empty;
                string questionTitle = string.Empty;
                int questionId = 0;
                
                // Find the recipient based on target type
                if (targetType.Equals("Question", StringComparison.OrdinalIgnoreCase))
                {
                    var question = await _context.Questions.FindAsync(targetId);
                    if (question != null && question.UserId != userId) // Don't notify yourself
                    {
                        recipientId = question.UserId;
                        title = question.Title;
                        questionTitle = question.Title;
                        questionId = question.QuestionId;
                    }
                }
                else if (targetType.Equals("Answer", StringComparison.OrdinalIgnoreCase))
                {
                    var answer = await _context.Answers
                        .Include(a => a.Question)
                        .FirstOrDefaultAsync(a => a.AnswerId == targetId);
                    
                    if (answer != null && answer.UserId != userId) // Don't notify yourself
                    {
                        recipientId = answer.UserId;
                        title = "your answer";
                        
                        if (answer.Question != null)
                        {
                            questionTitle = answer.Question.Title;
                            questionId = answer.Question.QuestionId;
                        }
                    }
                }

                if (recipientId.HasValue && questionId > 0)
                {
                    var notification = new Notification
                    {
                        UserId = recipientId,
                        Title = "New Comment",
                        Message = $"Someone commented on {title}.",
                        Url = $"/Questions/Details/{questionId}#comment-{commentId}",
                        IsRead = false,
                        CreatedDate = DateTime.Now,
                        NotificationType = NotificationTypeConstants.Comment,
                        RelatedEntityId = commentId
                    };

                    // Add to groups for SignalR
                    var recipientGroups = new List<string>
                    {
                        $"question-{questionId}"
                    };

                    await CreateNotificationAsync(notification, recipientGroups);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating comment notification for {targetType} {targetId}");
            }
        }

        /// <summary>
        /// Create a notification when a vote is cast
        /// </summary>
        public async Task NotifyVoteAsync(string targetType, int targetId, int userId)
        {
            try
            {
                int? recipientId = null;
                string contentType = string.Empty;
                string url = string.Empty;
                var recipientGroups = new List<string>();
                
                // Find the recipient based on target type
                if (targetType.Equals("Question", StringComparison.OrdinalIgnoreCase))
                {
                    var question = await _context.Questions.FindAsync(targetId);
                    if (question != null && question.UserId != userId) // Don't notify yourself
                    {
                        recipientId = question.UserId;
                        contentType = "question";
                        url = $"/Questions/Details/{targetId}";
                        
                        // Add specific group for question upvotes
                        recipientGroups.Add($"user-{question.UserId}");
                        recipientGroups.Add($"question-upvote-{targetId}");
                    }
                }
                else if (targetType.Equals("Answer", StringComparison.OrdinalIgnoreCase))
                {
                    var answer = await _context.Answers
                        .Include(a => a.Question)
                        .FirstOrDefaultAsync(a => a.AnswerId == targetId);
                    
                    if (answer != null && answer.UserId != userId) // Don't notify yourself
                    {
                        recipientId = answer.UserId;
                        contentType = "answer";
                        
                        if (answer.Question != null)
                        {
                            url = $"/Questions/Details/{answer.Question.QuestionId}#answer-{targetId}";
                            
                            // Add specific group for answer upvotes
                            recipientGroups.Add($"user-{answer.UserId}");
                            recipientGroups.Add($"answer-upvote-{targetId}");
                        }
                    }
                }

                if (recipientId.HasValue && !string.IsNullOrEmpty(url))
                {
                    var notification = new Notification
                    {
                        UserId = recipientId,
                        Title = "Vote Received",
                        Message = $"Someone voted on your {contentType}.",
                        Url = url,
                        IsRead = false,
                        CreatedDate = DateTime.Now,
                        NotificationType = NotificationTypeConstants.Vote,
                        RelatedEntityId = targetId
                    };

                    await CreateNotificationAsync(notification, recipientGroups);
                    _logger.LogInformation($"Vote notification created for {targetType} {targetId}, sent to user {recipientId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating vote notification for {targetType} {targetId}");
            }
        }

        /// <summary>
        /// Create a notification when an answer is accepted
        /// </summary>
        public async Task NotifyAnswerAcceptedAsync(int questionId, int answerId)
        {
            try
            {
                var answer = await _context.Answers.FindAsync(answerId);
                if (answer == null)
                    return;

                var question = await _context.Questions.FindAsync(questionId);
                if (question == null)
                    return;

                // Don't notify if question owner is the same as answer owner
                if (question.UserId == answer.UserId)
                    return;

                var notification = new Notification
                {
                    UserId = answer.UserId,
                    Title = "Answer Accepted",
                    Message = $"Your answer to '{question.Title}' has been accepted as the solution.",
                    Url = $"/Questions/Details/{questionId}#answer-{answerId}",
                    IsRead = false,
                    CreatedDate = DateTime.Now,
                    NotificationType = NotificationTypeConstants.Accept,
                    RelatedEntityId = answerId
                };

                // Add to groups for SignalR
                var recipientGroups = new List<string>
                {
                    $"question-{questionId}"
                };

                await CreateNotificationAsync(notification, recipientGroups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating answer-accepted notification for answer {answerId}");
            }
        }
    }
} 