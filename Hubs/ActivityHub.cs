using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace DoAnWeb.Hubs
{
    /// <summary>
    /// SignalR hub for real-time site activity and statistics
    /// </summary>
    public class ActivityHub : Hub
    {
        private static readonly ConcurrentDictionary<string, DateTime> ActiveConnections = new ConcurrentDictionary<string, DateTime>();
        private static readonly ConcurrentDictionary<string, string> ConnectionPages = new ConcurrentDictionary<string, string>();
        private static int TotalPageViews = 0;
        private readonly ILogger<ActivityHub> _logger;

        public ActivityHub(ILogger<ActivityHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// When a client connects, track as active user
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            try
            {
                // Record connection
                ActiveConnections[Context.ConnectionId] = DateTime.UtcNow;
                
                // Add to general activity group
                await Groups.AddToGroupAsync(Context.ConnectionId, "activity-feed");
                
                // Send current activity stats
                await SendActivityStats();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ActivityHub.OnConnectedAsync");
            }
            
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// When a client disconnects, remove from tracking
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                // Remove connection
                ActiveConnections.TryRemove(Context.ConnectionId, out _);
                ConnectionPages.TryRemove(Context.ConnectionId, out _);
                
                // Update stats for others
                await SendActivityStats();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ActivityHub.OnDisconnectedAsync");
            }
            
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Record a page view
        /// </summary>
        public async Task PageView(string pagePath, string pageTitle)
        {
            try
            {
                // Update connection's current page
                ConnectionPages[Context.ConnectionId] = pagePath;
                
                // Increment page views
                System.Threading.Interlocked.Increment(ref TotalPageViews);
                
                // Broadcast to activity feed
                await Clients.Group("activity-feed").SendAsync("NewPageView", new 
                {
                    PagePath = pagePath,
                    PageTitle = pageTitle,
                    Timestamp = DateTime.UtcNow
                });
                
                // Send updated stats
                await SendActivityStats();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording page view");
            }
        }

        /// <summary>
        /// Send current activity statistics to clients
        /// </summary>
        private async Task SendActivityStats()
        {
            var stats = new
            {
                ActiveConnections = ActiveConnections.Count,
                TotalPageViews = TotalPageViews,
                Timestamp = DateTime.UtcNow
            };
            
            await Clients.Group("activity-feed").SendAsync("ActivityStats", stats);
        }

        /// <summary>
        /// Subscribe to admin-only activity feed
        /// </summary>
        public async Task SubscribeToDetailedFeed()
        {
            // In a real app, check if user is admin
            // For now, allow anyone to subscribe
            await Groups.AddToGroupAsync(Context.ConnectionId, "admin-activity");
        }

        /// <summary>
        /// Record an action on the site (like, comment, etc.)
        /// </summary>
        public async Task RecordAction(string actionType, string targetId, string details)
        {
            try
            {
                var activity = new
                {
                    ConnectionId = Context.ConnectionId,
                    ActionType = actionType,
                    TargetId = targetId,
                    Details = details,
                    Timestamp = DateTime.UtcNow
                };
                
                // Send to regular activity feed with limited details
                await Clients.Group("activity-feed").SendAsync("ActivityAction", new
                {
                    ActionType = actionType,
                    Timestamp = DateTime.UtcNow
                });
                
                // Send full details to admin feed
                await Clients.Group("admin-activity").SendAsync("DetailedActivity", activity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error recording action {actionType}");
            }
        }
    }
} 