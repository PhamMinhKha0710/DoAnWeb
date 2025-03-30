using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using DoAnWeb.Models;
using DoAnWeb.Repositories;
using DoAnWeb.Services;
using System.Security.Claims;
using System.Linq;
using System.Collections.Generic;

namespace DoAnWeb.Filters
{
    public class UserInfoFilter : IActionFilter
    {
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;

        public UserInfoFilter(IUserRepository userRepository, INotificationService notificationService)
        {
            _userRepository = userRepository;
            _notificationService = notificationService;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Không làm gì ở đây
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Chỉ thiết lập ViewBag cho các kết quả ViewResult (không áp dụng cho API hoặc các kết quả khác)
            if (context.Result is ViewResult viewResult)
            {
                // Kiểm tra xem người dùng đã xác thực chưa
                if (context.HttpContext.User?.Identity != null && context.HttpContext.User.Identity.IsAuthenticated)
                {
                    // Lấy ID người dùng từ claims
                    var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                    {
                        // Lấy số lượng thông báo chưa đọc và đặt vào ViewBag
                        try
                        {
                            // 1. Lấy số lượng thông báo chưa đọc
                            var unreadCount = _notificationService.GetUnreadNotificationCountAsync(userId).GetAwaiter().GetResult();
                            viewResult.ViewData["UnreadNotificationsCount"] = unreadCount;
                            
                            // 2. Lấy danh sách thông báo gần đây (10 thông báo mới nhất)
                            var recentNotifications = _notificationService.GetUserNotificationsAsync(userId).GetAwaiter().GetResult()
                                .OrderByDescending(n => n.CreatedDate)
                                .Take(10)
                                .Select(n => new {
                                    Title = n.Title,
                                    Message = n.Message,
                                    Url = n.Url,
                                    CreatedDate = n.CreatedDate,
                                    IsRead = n.IsRead,
                                    Type = GetNotificationType(n),
                                    TimeAgo = FormatTimeAgo(n.CreatedDate)
                                })
                                .ToList();

                            // Đặt danh sách thông báo vào ViewBag
                            viewResult.ViewData["RecentNotifications"] = recentNotifications;
                            
                            // In ra console để debug
                            System.Diagnostics.Debug.WriteLine($"Unread notifications count for user {userId}: {unreadCount}");
                            System.Diagnostics.Debug.WriteLine($"Recent notifications count: {recentNotifications.Count}");
                        }
                        catch (Exception ex)
                        {
                            // Ghi log lỗi nhưng không dừng xử lý
                            System.Diagnostics.Debug.WriteLine($"Error getting notifications: {ex.Message}");
                            viewResult.ViewData["UnreadNotificationsCount"] = 0;
                            viewResult.ViewData["RecentNotifications"] = new List<object>();
                        }
                        
                        // Lấy thông tin người dùng từ repository
                        var user = _userRepository.GetById(userId);
                        if (user != null)
                        {
                            // Thiết lập avatar người dùng vào ViewBag
                            string avatar = null;
                            
                            // Ưu tiên ProfilePicture (hình ảnh được tải lên)
                            if (!string.IsNullOrEmpty(user.ProfilePicture))
                            {
                                avatar = user.ProfilePicture;
                            }
                            // Sau đó kiểm tra AvatarUrl (thường là từ dịch vụ bên ngoài)
                            else if (!string.IsNullOrEmpty(user.AvatarUrl))
                            {
                                avatar = user.AvatarUrl;
                            }
                            
                            // Thiết lập vào ViewBag
                            viewResult.ViewData["CurrentUserAvatar"] = avatar;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Chuyển đổi từ thời gian thành chuỗi "... ago"
        /// </summary>
        private string FormatTimeAgo(DateTime? dateTime)
        {
            if (!dateTime.HasValue)
                return "just now";
                
            var date = dateTime.Value;
            var now = DateTime.UtcNow;
            var span = now - date;
            
            if (span.TotalDays > 365)
            {
                int years = (int)(span.TotalDays / 365);
                return $"{years} {(years == 1 ? "year" : "years")} ago";
            }
            if (span.TotalDays > 30)
            {
                int months = (int)(span.TotalDays / 30);
                return $"{months} {(months == 1 ? "month" : "months")} ago";
            }
            if (span.TotalDays > 1)
            {
                return $"{(int)span.TotalDays} {((int)span.TotalDays == 1 ? "day" : "days")} ago";
            }
            if (span.TotalHours > 1)
            {
                return $"{(int)span.TotalHours} {((int)span.TotalHours == 1 ? "hour" : "hours")} ago";
            }
            if (span.TotalMinutes > 1)
            {
                return $"{(int)span.TotalMinutes} {((int)span.TotalMinutes == 1 ? "minute" : "minutes")} ago";
            }
            
            return "just now";
        }
        
        /// <summary>
        /// Xác định loại thông báo
        /// </summary>
        private string GetNotificationType(Notification notification)
        {
            // Xác định loại thông báo dựa trên Title, Message, hoặc các thông tin khác
            string title = notification.Title?.ToLower() ?? "";
            string message = notification.Message?.ToLower() ?? "";
            
            if (title.Contains("upvote") || message.Contains("upvote") || message.Contains("vote"))
                return "vote";
                
            if (title.Contains("answer") || message.Contains("answer") || message.Contains("answered"))
                return "question_answer";
                
            if (title.Contains("comment") || message.Contains("comment") || message.Contains("commented"))
                return "comment";
                
            if (title.Contains("mention") || message.Contains("mention") || message.Contains("mentioned") || message.Contains("@"))
                return "mention";
                
            if (title.Contains("accept") || message.Contains("accept") || message.Contains("accepted"))
                return "accept";
                
            return "system";
        }
    }
} 