using DoAnWeb.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DoAnWeb.Services
{
    /// <summary>
    /// Interface for notification service operations
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Get all notifications for a user
        /// </summary>
        Task<List<Notification>> GetUserNotificationsAsync(int userId);
        
        /// <summary>
        /// Get unread notifications for a user
        /// </summary>
        Task<List<Notification>> GetUnreadNotificationsAsync(int userId);
        
        /// <summary>
        /// Get unread notifications count for a user
        /// </summary>
        Task<int> GetUnreadNotificationCountAsync(int userId);
        
        /// <summary>
        /// Mark a notification as read
        /// </summary>
        Task<bool> MarkAsReadAsync(int notificationId, int userId);
        
        /// <summary>
        /// Mark all notifications as read for a user
        /// </summary>
        Task<int> MarkAllAsReadAsync(int userId);
        
        /// <summary>
        /// Create a notification
        /// </summary>
        Task<Notification> CreateNotificationAsync(Notification notification, List<string> recipientGroups = null);
        
        /// <summary>
        /// Create an answer notification
        /// </summary>
        Task NotifyNewAnswerAsync(int questionId, int answerId, int userId);
        
        /// <summary>
        /// Create a comment notification
        /// </summary>
        Task NotifyNewCommentAsync(string targetType, int targetId, int commentId, int userId);
        
        /// <summary>
        /// Create a vote notification
        /// </summary>
        Task NotifyVoteAsync(string targetType, int targetId, int userId);
        
        /// <summary>
        /// Create an accept answer notification
        /// </summary>
        Task NotifyAnswerAcceptedAsync(int questionId, int answerId);
    }
} 