using DoAnWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DoAnWeb.Hubs
{
    /// <summary>
    /// SignalR hub for handling real-time badge progress updates
    /// </summary>
    [Authorize]
    public class BadgeHub : Hub
    {
        private readonly DevCommunityContext _context;
        private readonly ILogger<BadgeHub> _logger;

        public BadgeHub(DevCommunityContext context, ILogger<BadgeHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// When a user connects, join them to their personal badge group
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            try
            {
                // Get current user ID from claims
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    // Connect user to their personal badge group
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"badge-user-{userId}");
                    _logger.LogInformation($"User {userId} connected to BadgeHub");
                }
                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BadgeHub.OnConnectedAsync");
                await base.OnConnectedAsync();
            }
        }

        /// <summary>
        /// Disconnect from the hub
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"badge-user-{userId}");
                    _logger.LogInformation($"User {userId} disconnected from BadgeHub");
                }
                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BadgeHub.OnDisconnectedAsync");
                await base.OnDisconnectedAsync(exception);
            }
        }
    }
} 