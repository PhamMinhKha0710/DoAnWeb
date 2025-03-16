using DoAnWeb.Models;
using DoAnWeb.Hubs;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace DoAnWeb.Services
{
    /// <summary>
    /// Background service for processing notifications asynchronously
    /// </summary>
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ConcurrentQueue<NotificationQueueItem> _queue;
        private readonly ILogger<NotificationBackgroundService> _logger;

        public NotificationBackgroundService(
            IServiceScopeFactory serviceScopeFactory,
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationBackgroundService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _hubContext = hubContext;
            _queue = new ConcurrentQueue<NotificationQueueItem>();
            _logger = logger;
        }

        /// <summary>
        /// Add a notification to the processing queue
        /// </summary>
        public void QueueNotification(NotificationQueueItem notificationItem)
        {
            _queue.Enqueue(notificationItem);
        }

        /// <summary>
        /// Background processing method that runs continuously
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessNotificationsAsync();
                await Task.Delay(1000, stoppingToken); // Process queue every second
            }

            _logger.LogInformation("Notification Background Service is stopping.");
        }

        /// <summary>
        /// Process notifications from the queue
        /// </summary>
        private async Task ProcessNotificationsAsync()
        {
            try
            {
                int count = 0;
                // Process up to 50 notifications at once to avoid blocking
                while (count < 50 && _queue.TryDequeue(out var notificationItem))
                {
                    await ProcessNotificationItemAsync(notificationItem);
                    count++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification queue");
            }
        }

        /// <summary>
        /// Process a single notification
        /// </summary>
        private async Task ProcessNotificationItemAsync(NotificationQueueItem notificationItem)
        {
            try
            {
                // Create a new scope to resolve services
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<DevCommunityContext>();
                
                // Save to database
                dbContext.Notifications.Add(notificationItem.Notification);
                await dbContext.SaveChangesAsync();

                // Send real-time notification via SignalR
                if (notificationItem.SendRealTime)
                {
                    if (notificationItem.RecipientGroups != null && notificationItem.RecipientGroups.Any())
                    {
                        // Send to specific groups
                        foreach (var group in notificationItem.RecipientGroups)
                        {
                            await _hubContext.Clients.Group(group).SendAsync(
                                "ReceiveNotification", 
                                new 
                                {
                                    id = notificationItem.Notification.NotificationId,
                                    title = notificationItem.Notification.Title,
                                    message = notificationItem.Notification.Message,
                                    url = notificationItem.Notification.Url,
                                    type = notificationItem.Notification.NotificationType,
                                    createdDate = notificationItem.Notification.CreatedDate
                                });
                        }
                    }
                    else if (notificationItem.Notification.UserId.HasValue)
                    {
                        // Send to specific user
                        var userId = notificationItem.Notification.UserId.Value.ToString();
                        await _hubContext.Clients.User(userId).SendAsync(
                            "ReceiveNotification", 
                            new 
                            {
                                id = notificationItem.Notification.NotificationId,
                                title = notificationItem.Notification.Title,
                                message = notificationItem.Notification.Message,
                                url = notificationItem.Notification.Url,
                                type = notificationItem.Notification.NotificationType,
                                createdDate = notificationItem.Notification.CreatedDate
                            });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification item");
            }
        }
    }

    /// <summary>
    /// Class representing an item in the notification queue
    /// </summary>
    public class NotificationQueueItem
    {
        public Notification Notification { get; set; }
        public bool SendRealTime { get; set; } = true;
        public List<string> RecipientGroups { get; set; } = new List<string>();
    }
} 