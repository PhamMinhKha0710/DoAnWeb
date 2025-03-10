using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DoAnWeb.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace DoAnWeb.Controllers
{
    [Authorize]
    public class TagPreferencesController : Controller
    {
        private readonly DevCommunityContext _context;

        public TagPreferencesController(DevCommunityContext context)
        {
            _context = context;
        }

        // GET: TagPreferences/WatchedTags
        public IActionResult WatchedTags()
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToAction("Login", "Account");

            // Get user's watched tags
            var watchedTags = _context.UserWatchedTags
                .Include(w => w.Tag)
                .Where(w => w.UserId == userId)
                .Select(w => w.Tag)
                .ToList();

            // Get all available tags for dropdown
            ViewBag.AllTags = _context.Tags.ToList();

            return View(watchedTags);
        }

        // POST: TagPreferences/WatchTag
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult WatchTag(int tagId)
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToAction("Login", "Account");

            // Check if tag exists
            var tag = _context.Tags.Find(tagId);
            if (tag == null)
            {
                TempData["ErrorMessage"] = "Tag not found.";
                return RedirectToAction(nameof(WatchedTags));
            }

            // Check if user is already watching this tag
            var existingWatch = _context.UserWatchedTags
                .FirstOrDefault(w => w.UserId == userId && w.TagId == tagId);

            if (existingWatch == null)
            {
                // Add tag to watched tags
                var watchedTag = new UserWatchedTag
                {
                    UserId = userId,
                    TagId = tagId,
                    CreatedDate = DateTime.Now
                };

                _context.UserWatchedTags.Add(watchedTag);
                _context.SaveChanges();

                TempData["SuccessMessage"] = $"You are now watching the tag '{tag.TagName}'.";
            }
            else
            {
                TempData["InfoMessage"] = $"You are already watching the tag '{tag.TagName}'.";
            }

            return RedirectToAction(nameof(WatchedTags));
        }

        // POST: TagPreferences/UnwatchTag
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UnwatchTag(int tagId)
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToAction("Login", "Account");

            // Find the watched tag entry
            var watchedTag = _context.UserWatchedTags
                .Include(w => w.Tag)
                .FirstOrDefault(w => w.UserId == userId && w.TagId == tagId);

            if (watchedTag != null)
            {
                // Remove tag from watched tags
                _context.UserWatchedTags.Remove(watchedTag);
                _context.SaveChanges();

                TempData["SuccessMessage"] = $"You are no longer watching the tag '{watchedTag.Tag.TagName}'.";
            }
            else
            {
                TempData["ErrorMessage"] = "You are not watching this tag.";
            }

            return RedirectToAction(nameof(WatchedTags));
        }

        // GET: TagPreferences/IgnoredTags
        public IActionResult IgnoredTags()
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToAction("Login", "Account");

            // Get user's ignored tags
            var ignoredTags = _context.UserIgnoredTags
                .Include(i => i.Tag)
                .Where(i => i.UserId == userId)
                .Select(i => i.Tag)
                .ToList();

            // Get all available tags for dropdown
            ViewBag.AllTags = _context.Tags.ToList();

            return View(ignoredTags);
        }

        // POST: TagPreferences/IgnoreTag
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult IgnoreTag(int tagId)
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToAction("Login", "Account");

            // Check if tag exists
            var tag = _context.Tags.Find(tagId);
            if (tag == null)
            {
                TempData["ErrorMessage"] = "Tag not found.";
                return RedirectToAction(nameof(IgnoredTags));
            }

            // Check if user is already ignoring this tag
            var existingIgnore = _context.UserIgnoredTags
                .FirstOrDefault(i => i.UserId == userId && i.TagId == tagId);

            if (existingIgnore == null)
            {
                // Add tag to ignored tags
                var ignoredTag = new UserIgnoredTag
                {
                    UserId = userId,
                    TagId = tagId,
                    CreatedDate = DateTime.Now
                };

                _context.UserIgnoredTags.Add(ignoredTag);
                _context.SaveChanges();

                TempData["SuccessMessage"] = $"You are now ignoring the tag '{tag.TagName}'.";
            }
            else
            {
                TempData["InfoMessage"] = $"You are already ignoring the tag '{tag.TagName}'.";
            }

            return RedirectToAction(nameof(IgnoredTags));
        }

        // POST: TagPreferences/UnignoreTag
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UnignoreTag(int tagId)
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToAction("Login", "Account");

            // Find the ignored tag entry
            var ignoredTag = _context.UserIgnoredTags
                .Include(i => i.Tag)
                .FirstOrDefault(i => i.UserId == userId && i.TagId == tagId);

            if (ignoredTag != null)
            {
                // Remove tag from ignored tags
                _context.UserIgnoredTags.Remove(ignoredTag);
                _context.SaveChanges();

                TempData["SuccessMessage"] = $"You are no longer ignoring the tag '{ignoredTag.Tag.TagName}'.";
            }
            else
            {
                TempData["ErrorMessage"] = "You are not ignoring this tag.";
            }

            return RedirectToAction(nameof(IgnoredTags));
        }
    }
}