using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DoAnWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace DoAnWeb.Hubs
{
    /// <summary>
    /// SignalR hub for tracking user presence and online status
    /// </summary>
    [Authorize]
    public class PresenceHub : Hub
    {
        private static readonly Dictionary<string, HashSet<string>> OnlineUsers = new Dictionary<string, HashSet<string>>();
        private readonly ILogger<PresenceHub> _logger;
        private readonly DevCommunityContext _context;

        public PresenceHub(ILogger<PresenceHub> logger, DevCommunityContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// When a user connects, track their online status
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

                if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
                {
                    // Add to tracking collection
                    lock (OnlineUsers)
                    {
                        if (!OnlineUsers.ContainsKey(userId))
                        {
                            OnlineUsers[userId] = new HashSet<string>();
                            // First connection for this user, broadcast they are now online
                            _ = Clients.Others.SendAsync("UserOnline", new { UserId = userId, Username = username });
                        }
                        OnlineUsers[userId].Add(Context.ConnectionId);
                    }

                    // Add user to their own group for targeting
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
                    
                    // Update user's ConnectionId in the database
                    var user = await _context.Users.FindAsync(userIdInt);
                    if (user != null)
                    {
                        user.ConnectionId = Context.ConnectionId;
                        await _context.SaveChangesAsync();
                    }

                    // Send current online users list to the connecting client
                    await SendOnlineUsers();
                }

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PresenceHub.OnConnectedAsync");
                await base.OnConnectedAsync();
            }
        }

        /// <summary>
        /// When a user disconnects, update their online status
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

                if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
                {
                    bool isOffline = false;

                    lock (OnlineUsers)
                    {
                        if (OnlineUsers.ContainsKey(userId))
                        {
                            // Remove this connection
                            OnlineUsers[userId].Remove(Context.ConnectionId);

                            // If no more connections, user is offline
                            if (OnlineUsers[userId].Count == 0)
                            {
                                OnlineUsers.Remove(userId);
                                isOffline = true;
                            }
                        }
                    }
                    
                    // Clear user's ConnectionId in the database if they're offline
                    if (isOffline)
                    {
                        var user = await _context.Users.FindAsync(userIdInt);
                        if (user != null && user.ConnectionId == Context.ConnectionId)
                        {
                            user.ConnectionId = null;
                            await _context.SaveChangesAsync();
                        }
                        
                        // Broadcast to others that user is offline
                        await Clients.Others.SendAsync("UserOffline", new { UserId = userId, Username = username });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PresenceHub.OnDisconnectedAsync");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Send the list of currently online users to the caller
        /// </summary>
        private async Task SendOnlineUsers()
        {
            try
            {
                List<string> userIds;
                
                lock (OnlineUsers)
                {
                    userIds = OnlineUsers.Keys.ToList();
                }

                await Clients.Caller.SendAsync("OnlineUsers", userIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PresenceHub.SendOnlineUsers");
            }
        }

        /// <summary>
        /// Get the current count of online users
        /// </summary>
        public async Task GetOnlineCount()
        {
            int count;
            
            lock (OnlineUsers)
            {
                count = OnlineUsers.Count;
            }

            await Clients.Caller.SendAsync("OnlineCount", count);
        }
    }
} 