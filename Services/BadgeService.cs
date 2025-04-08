using DoAnWeb.Hubs;
using DoAnWeb.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnWeb.Services
{
    /// <summary>
    /// Service for managing badges and badge progress
    /// </summary>
    public class BadgeService : IBadgeService
    {
        private readonly DevCommunityContext _context;
        private readonly IHubContext<BadgeHub> _badgeHubContext;
        private readonly ILogger<BadgeService> _logger;

        public BadgeService(
            DevCommunityContext context,
            IHubContext<BadgeHub> badgeHubContext,
            ILogger<BadgeService> logger)
        {
            _context = context;
            _badgeHubContext = badgeHubContext;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<List<BadgeProgress>> GetUserBadgeProgressAsync(int userId, int count = 2)
        {
            try
            {
                // Get all badges
                var badges = await _context.Badges
                    .Where(b => b.Status == "Active")
                    .ToListAsync();

                // Get badges the user has already earned
                var earnedBadgeIds = await _context.BadgeAssignments
                    .Where(ba => ba.UserId == userId)
                    .Select(ba => ba.BadgeId)
                    .ToListAsync();

                var badgeProgress = new List<BadgeProgress>();

                // Calculate progress for each badge
                foreach (var badge in badges)
                {
                    var progress = await CalculateBadgeProgressAsync(userId, badge);
                    badgeProgress.Add(progress);
                }

                // Return top unearned badges by progress percentage
                return badgeProgress
                    .Where(bp => !bp.IsEarned) // Filter to unearned badges
                    .OrderByDescending(bp => (double)bp.CurrentCount / bp.TargetCount) // Order by progress percentage
                    .Take(count)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting badge progress for user {UserId}", userId);
                return new List<BadgeProgress>();
            }
        }

        /// <inheritdoc />
        public async Task<bool> UpdateBadgeProgressAsync(int userId, int badgeId)
        {
            try
            {
                var badge = await _context.Badges.FindAsync(badgeId);
                if (badge == null)
                {
                    _logger.LogWarning("Badge {BadgeId} not found", badgeId);
                    return false;
                }

                // Check if user already earned this badge
                var badgeAssignment = await _context.BadgeAssignments
                    .FirstOrDefaultAsync(ba => ba.UserId == userId && ba.BadgeId == badgeId);
                
                if (badgeAssignment != null)
                {
                    _logger.LogInformation("User {UserId} already earned badge {BadgeId}", userId, badgeId);
                    return false;
                }

                // Calculate current progress
                var progress = await CalculateBadgeProgressAsync(userId, badge);
                
                // Notify clients of the updated progress
                await NotifyBadgeProgressUpdateAsync(userId, progress);

                // Check if badge should be awarded
                if (progress.CurrentCount >= progress.TargetCount)
                {
                    await AwardBadgeAsync(userId, badgeId, $"Completed {progress.Name} badge requirements");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating badge progress for user {UserId} and badge {BadgeId}", userId, badgeId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<List<BadgeProgress>> RecalculateAllBadgeProgressAsync(int userId)
        {
            try
            {
                var badges = await _context.Badges
                    .Where(b => b.Status == "Active")
                    .ToListAsync();

                var progressUpdates = new List<BadgeProgress>();

                foreach (var badge in badges)
                {
                    var progress = await CalculateBadgeProgressAsync(userId, badge);
                    progressUpdates.Add(progress);

                    // Notify clients of the updated progress
                    await NotifyBadgeProgressUpdateAsync(userId, progress);

                    // Check if badge should be awarded
                    if (!progress.IsEarned && progress.CurrentCount >= progress.TargetCount)
                    {
                        await AwardBadgeAsync(userId, badge.BadgeId, $"Completed {progress.Name} badge requirements");
                    }
                }

                return progressUpdates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating badge progress for user {UserId}", userId);
                return new List<BadgeProgress>();
            }
        }

        /// <inheritdoc />
        public async Task<bool> AwardBadgeAsync(int userId, int badgeId, string reason = "")
        {
            try
            {
                // Check if user already has the badge
                var existingAssignment = await _context.BadgeAssignments
                    .FirstOrDefaultAsync(ba => ba.UserId == userId && ba.BadgeId == badgeId);

                if (existingAssignment != null)
                {
                    _logger.LogInformation("User {UserId} already has badge {BadgeId}", userId, badgeId);
                    return false;
                }

                var badge = await _context.Badges.FindAsync(badgeId);
                if (badge == null)
                {
                    _logger.LogWarning("Badge {BadgeId} not found", badgeId);
                    return false;
                }

                // Award the badge
                var badgeAssignment = new BadgeAssignment
                {
                    UserId = userId,
                    BadgeId = badgeId,
                    AwardedDate = DateTime.Now,
                    Reason = reason,
                    Notified = false
                };

                await _context.BadgeAssignments.AddAsync(badgeAssignment);
                
                // Award reputation points
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    // Thay vì gán trực tiếp vào Reputation, kiểm tra xem có phương thức cập nhật
                    // user.Reputation += badge.ReputationBonus;
                    
                    // Sử dụng trường dữ liệu thay vì property nếu có
                    user.ReputationPoints += badge.ReputationBonus;
                    // Hoặc tạo một phương thức AddReputation nếu có
                    // user.AddReputation(badge.ReputationBonus);
                }

                await _context.SaveChangesAsync();

                // Notify clients that the badge has been awarded
                await NotifyBadgeAwardedAsync(userId, badge);

                _logger.LogInformation("Badge {BadgeId} awarded to user {UserId}", badgeId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error awarding badge {BadgeId} to user {UserId}", badgeId, userId);
                return false;
            }
        }

        // Helper methods

        /// <summary>
        /// Calculates the progress for a specific badge for a user
        /// </summary>
        private async Task<BadgeProgress> CalculateBadgeProgressAsync(int userId, Badge badge)
        {
            // Check if user already earned this badge
            var badgeAssignment = await _context.BadgeAssignments
                .FirstOrDefaultAsync(ba => ba.UserId == userId && ba.BadgeId == badge.BadgeId);
            
            var isEarned = badgeAssignment != null;

            // Initialize default progress values
            int currentCount = 0;
            int targetCount = 1;
            string iconClass = "bi-award-fill";
            string colorClass = "bg-primary";

            // Calculate progress based on badge name and criteria
            // This is a simplified implementation - in a real app, you'd have more sophisticated logic
            // based on the badge criteria stored in the database
            switch (badge.Name.ToLower())
            {
                case "curious":
                    targetCount = 5;
                    iconClass = "bi-trophy-fill";
                    colorClass = "bg-warning";
                    // Count well-received questions (score > 0)
                    currentCount = await _context.Questions
                        .CountAsync(q => q.UserId == userId && q.Score > 0);
                    break;

                case "helper":
                    targetCount = 3;
                    iconClass = "bi-chat-dots-fill";
                    colorClass = "bg-info";
                    // Count well-received answers (score >= 3)
                    currentCount = await _context.Answers
                        .CountAsync(a => a.UserId == userId && a.Score >= 3);
                    break;

                case "editor":
                    targetCount = 10;
                    iconClass = "bi-pencil-fill";
                    colorClass = "bg-success";
                    // Count edits to questions or answers
                    var questionEdits = await _context.Questions
                        .CountAsync(q => q.UserId == userId && q.UpdatedDate != null);
                    var answerEdits = await _context.Answers
                        .CountAsync(a => a.UserId == userId && a.UpdatedDate != null);
                    currentCount = questionEdits + answerEdits;
                    break;

                // Add more badge types as needed
                
                default:
                    targetCount = 1;
                    break;
            }

            return new BadgeProgress
            {
                BadgeId = badge.BadgeId,
                Name = badge.Name,
                Description = badge.Description ?? "",
                Type = badge.Type ?? "Bronze",
                IconClass = iconClass,
                ColorClass = colorClass,
                CurrentCount = currentCount,
                TargetCount = targetCount,
                IsEarned = isEarned
            };
        }

        /// <summary>
        /// Notifies clients of a badge progress update
        /// </summary>
        private async Task NotifyBadgeProgressUpdateAsync(int userId, BadgeProgress progress)
        {
            try
            {
                await _badgeHubContext.Clients
                    .Group($"badge-user-{userId}")
                    .SendAsync("BadgeProgressUpdated", progress);

                _logger.LogInformation("Notified user {UserId} of badge progress update for {BadgeName}", userId, progress.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying user {UserId} of badge progress update", userId);
            }
        }

        /// <summary>
        /// Notifies clients that a badge has been awarded
        /// </summary>
        private async Task NotifyBadgeAwardedAsync(int userId, Badge badge)
        {
            try
            {
                var badgeInfo = new
                {
                    badge.BadgeId,
                    badge.Name,
                    badge.Description,
                    badge.Type,
                    badge.ReputationBonus,
                    AwardedDate = DateTime.Now
                };

                await _badgeHubContext.Clients
                    .Group($"badge-user-{userId}")
                    .SendAsync("BadgeAwarded", badgeInfo);

                _logger.LogInformation("Notified user {UserId} of badge award for {BadgeName}", userId, badge.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying user {UserId} of badge award", userId);
            }
        }
    }
} 