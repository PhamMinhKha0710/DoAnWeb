using DoAnWeb.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DoAnWeb.Services
{
    /// <summary>
    /// Service interface for managing badges and badge progress
    /// </summary>
    public interface IBadgeService
    {
        /// <summary>
        /// Gets the top badge progress items for a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="count">Number of badge progress items to return</param>
        /// <returns>List of badge progress items</returns>
        Task<List<BadgeProgress>> GetUserBadgeProgressAsync(int userId, int count = 2);
        
        /// <summary>
        /// Updates a user's badge progress and notifies clients if progress changed
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="badgeId">The badge ID</param>
        /// <returns>True if progress was updated, false otherwise</returns>
        Task<bool> UpdateBadgeProgressAsync(int userId, int badgeId);
        
        /// <summary>
        /// Recalculates all badge progress for a user and notifies clients if progress changed
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>List of updated badge progress items</returns>
        Task<List<BadgeProgress>> RecalculateAllBadgeProgressAsync(int userId);
        
        /// <summary>
        /// Awards a badge to a user if they've met the criteria
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="badgeId">The badge ID</param>
        /// <param name="reason">Reason for awarding the badge</param>
        /// <returns>True if badge was awarded, false otherwise</returns>
        Task<bool> AwardBadgeAsync(int userId, int badgeId, string reason = "");
    }
} 