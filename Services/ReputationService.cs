using DoAnWeb.Hubs;
using DoAnWeb.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace DoAnWeb.Services
{
    public class ReputationService
    {
        private readonly DevCommunityContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<ReputationService> _logger;

        public ReputationService(
            DevCommunityContext context,
            IHubContext<NotificationHub> hubContext,
            ILogger<ReputationService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Cập nhật reputation cho người dùng
        /// </summary>
        /// <param name="userId">ID người dùng</param>
        /// <param name="changeAmount">Lượng thay đổi (dương hoặc âm)</param>
        /// <param name="reason">Lý do thay đổi</param>
        /// <returns>Giá trị reputation mới</returns>
        public async Task<int> UpdateReputationAsync(int userId, int changeAmount, string reason)
        {
            try
            {
                // Tìm user và cập nhật reputation
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"Attempted to update reputation for non-existent user: {userId}");
                    return 0;
                }

                // Cập nhật điểm
                var oldReputationPoints = user.ReputationPoints;
                
                // Đảm bảo ReputationPoints không âm
                int newReputationPoints = Math.Max(0, oldReputationPoints + changeAmount);
                user.ReputationPoints = newReputationPoints;
                
                // Lưu lịch sử thay đổi reputation
                var history = new ReputationHistory
                {
                    UserId = userId,
                    Amount = changeAmount,
                    Reason = reason,
                    OldValue = oldReputationPoints,
                    NewValue = newReputationPoints,
                    CreatedDate = DateTime.Now
                };
                
                _context.ReputationHistories.Add(history);
                await _context.SaveChangesAsync();
                
                // Thông báo thay đổi qua SignalR
                await NotifyReputationChangeAsync(userId, newReputationPoints, reason, changeAmount);
                
                _logger.LogInformation($"Updated reputation for user {userId}: {oldReputationPoints} -> {newReputationPoints} ({changeAmount:+0;-#}) - Reason: {reason}");
                
                return newReputationPoints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating reputation for user {userId}");
                return 0;
            }
        }
        
        /// <summary>
        /// Cập nhật reputation dựa trên action trong hệ thống
        /// </summary>
        /// <param name="userId">ID người dùng</param>
        /// <param name="actionType">Loại hành động</param>
        /// <param name="entityId">ID của entity liên quan (câu hỏi, câu trả lời...)</param>
        /// <returns>Giá trị reputation mới</returns>
        public async Task<int> UpdateReputationForActionAsync(int userId, ReputationActionType actionType, int entityId)
        {
            var (changeAmount, reasonText) = GetReputationChangeDetails(actionType, entityId);
            return await UpdateReputationAsync(userId, changeAmount, reasonText);
        }
        
        /// <summary>
        /// Thông báo thay đổi reputation qua SignalR
        /// </summary>
        private async Task NotifyReputationChangeAsync(int userId, int newReputation, string reason, int changeAmount)
        {
            try
            {
                // Tạo prefix dấu cho thay đổi (+ hoặc -)
                string changePrefix = changeAmount > 0 ? "+" : "";
                
                // Gửi thông báo thay đổi reputation tới client
                await _hubContext.Clients.Group($"reputation-{userId}")
                    .SendAsync("ReputationChanged", userId, newReputation, reason);
                
                // Ghi nhận thông báo vào database để hiển thị trong notification center
                if (await ShouldCreateNotificationAsync(userId))
                {
                    var notification = new Notification
                    {
                        UserId = userId,
                        Title = "Thay đổi điểm uy tín",
                        Message = $"{changePrefix}{changeAmount} điểm uy tín: {reason}",
                        Url = $"/Users/Details/{userId}",
                        CreatedDate = DateTime.UtcNow,
                        IsRead = false,
                        NotificationType = NotificationTypeConstants.ReputationChange,
                        RelatedEntityId = userId // Sử dụng userId là entity đại diện
                    };
                    
                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error notifying reputation change for user {userId}");
            }
        }
        
        /// <summary>
        /// Kiểm tra xem có nên tạo notification cho thay đổi reputation không
        /// </summary>
        /// <remarks>
        /// Chỉ tạo notification cho những thay đổi đáng kể, 
        /// tránh spam notification cho người dùng
        /// </remarks>
        private async Task<bool> ShouldCreateNotificationAsync(int userId)
        {
            try
            {
                // Kiểm tra xem đã có thông báo gần đây chưa
                var recentNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && n.NotificationType == NotificationTypeConstants.ReputationChange)
                    .OrderByDescending(n => n.CreatedDate)
                    .Take(5)
                    .ToListAsync();
                
                // Nếu chưa có notification nào hoặc notification cuối cách đây hơn 30 phút
                if (recentNotifications.Count == 0 || 
                    (DateTime.Now - recentNotifications[0].CreatedDate.Value).TotalMinutes > 30)
                {
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if should create notification for user {userId}");
                return false;
            }
        }
        
        /// <summary>
        /// Lấy thông tin về lượng thay đổi reputation và lý do dựa vào loại action
        /// </summary>
        private (int amount, string reason) GetReputationChangeDetails(ReputationActionType actionType, int entityId)
        {
            return actionType switch
            {
                ReputationActionType.QuestionUpvoted => (10, $"Question upvoted (ID: {entityId})"),
                ReputationActionType.QuestionDownvoted => (-2, $"Question downvoted (ID: {entityId})"),
                ReputationActionType.AnswerUpvoted => (10, $"Answer upvoted (ID: {entityId})"),
                ReputationActionType.AnswerDownvoted => (-2, $"Answer downvoted (ID: {entityId})"),
                ReputationActionType.AnswerAccepted => (15, $"Answer accepted (ID: {entityId})"),
                ReputationActionType.AcceptingAnswer => (2, $"Accepting an answer (ID: {entityId})"),
                ReputationActionType.QuestionFavorited => (5, $"Question favorited (ID: {entityId})"),
                ReputationActionType.BountyAwarded => (50, $"Bounty awarded (ID: {entityId})"),
                ReputationActionType.BountyRemoved => (-50, $"Bounty removed (ID: {entityId})"),
                ReputationActionType.AdminAdjustment => (0, $"Admin adjustment (ID: {entityId})"),
                _ => (0, "Unspecified reason")
            };
        }
    }
    
    /// <summary>
    /// Enum định nghĩa các loại hành động ảnh hưởng đến reputation
    /// </summary>
    public enum ReputationActionType
    {
        QuestionUpvoted,
        QuestionDownvoted,
        AnswerUpvoted,
        AnswerDownvoted,
        AnswerAccepted,
        AcceptingAnswer,
        QuestionFavorited,
        BountyAwarded,
        BountyRemoved,
        AdminAdjustment
    }
} 