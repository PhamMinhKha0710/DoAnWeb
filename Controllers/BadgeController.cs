using DoAnWeb.Models;
using DoAnWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DoAnWeb.Controllers
{
    /// <summary>
    /// Controller for badge-related operations
    /// </summary>
    [Authorize]
    public class BadgeController : Controller
    {
        private readonly IBadgeService _badgeService;
        private readonly ILogger<BadgeController> _logger;

        public BadgeController(IBadgeService badgeService, ILogger<BadgeController> logger)
        {
            _badgeService = badgeService;
            _logger = logger;
        }

        /// <summary>
        /// Display the badge management page
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
                return RedirectToAction("Login", "Account");

            var badgeProgress = await _badgeService.GetUserBadgeProgressAsync(userId, 10);
            return View(badgeProgress);
        }

        /// <summary>
        /// API endpoint to get badge progress
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetBadgeProgress()
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized();

            var badgeProgress = await _badgeService.GetUserBadgeProgressAsync(userId, 10);
            return Ok(badgeProgress);
        }

        /// <summary>
        /// API endpoint to recalculate all badge progress
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RecalculateProgress()
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized();

            var badgeProgress = await _badgeService.RecalculateAllBadgeProgressAsync(userId);
            return Ok(badgeProgress);
        }

        /// <summary>
        /// API endpoint to simulate progress on a specific badge (for testing)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SimulateProgress(int badgeId = 1)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized();

            await _badgeService.UpdateBadgeProgressAsync(userId, badgeId);
            return Ok(new { success = true });
        }

        /// <summary>
        /// API endpoint to award a badge (for testing)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AwardBadge(int badgeId, int userId)
        {
            if (!User.IsInRole("Admin"))
                return Forbid();

            var result = await _badgeService.AwardBadgeAsync(userId, badgeId, "Manually awarded by admin");
            return Ok(new { success = result });
        }

        /// <summary>
        /// Helper method to get the current user ID
        /// </summary>
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return -1;

            return userId;
        }
    }
} 