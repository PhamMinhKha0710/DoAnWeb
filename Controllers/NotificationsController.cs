using DoAnWeb.Models;
using DoAnWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DoAnWeb.Controllers
{
    /// <summary>
    /// Controller for handling notification-related actions
    /// </summary>
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            INotificationService notificationService,
            ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Display all notifications for the current user
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get current user ID from claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                // Get all notifications for user
                var notifications = await _notificationService.GetUserNotificationsAsync(userId);
                return View(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications");
                TempData["ErrorMessage"] = "Error loading notifications. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Get unread notification count for the current user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            try
            {
                // Get current user ID from claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Json(new { count = 0 });
                }

                // Get count of unread notifications
                var count = await _notificationService.GetUnreadNotificationCountAsync(userId);
                return Json(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unread notification count");
                return Json(new { count = 0 });
            }
        }

        /// <summary>
        /// Mark a notification as read (AJAX endpoint)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                // Get current user ID from claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Mark notification as read
                var success = await _notificationService.MarkAsReadAsync(id, userId);
                if (success)
                {
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Could not mark notification as read" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking notification {id} as read");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// Mark all notifications as read (AJAX endpoint)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                // Get current user ID from claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Mark all notifications as read
                var count = await _notificationService.MarkAllAsReadAsync(userId);
                return Json(new { success = true, count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return Json(new { success = false, message = "An error occurred" });
            }
        }
    }
} 