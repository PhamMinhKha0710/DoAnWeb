using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DoAnWeb.Models;
using DoAnWeb.Services;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace DoAnWeb.Controllers
{
    public class TagsController : Controller
    {
        private readonly DevCommunityContext _context;
        private readonly IQuestionService _questionService;

        public TagsController(DevCommunityContext context, IQuestionService questionService)
        {
            _context = context;
            _questionService = questionService;
        }

        // GET: Tags
        public IActionResult Index(string searchQuery = null)
        {
            IQueryable<Tag> tagsQuery = _context.Tags.Include(t => t.QuestionTags);
            
            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                searchQuery = searchQuery.Trim().ToLower();
                tagsQuery = tagsQuery.Where(t => 
                    t.TagName.ToLower().Contains(searchQuery) || 
                    (t.Description != null && t.Description.ToLower().Contains(searchQuery))
                );
                
                ViewBag.SearchQuery = searchQuery;
            }
            
            var tags = tagsQuery.ToList();
            return View(tags);
        }

        // GET: Tags/Details/5
        public IActionResult Details(int id)
        {
            var tag = _context.Tags.FirstOrDefault(t => t.TagId == id);
            if (tag == null)
            {
                return NotFound();
            }

            ViewBag.Questions = _questionService.GetQuestionsByTag(tag.TagName);
            return View(tag);
        }

        // GET: Tags/GetTagsJson
        [HttpGet]
        public IActionResult GetTagsJson()
        {
            // Get all tag names for autocomplete suggestions
            var tagNames = _context.Tags
                .OrderByDescending(t => t.QuestionTags.Count)
                .Select(t => t.TagName)
                .ToList();
                
            return Json(tagNames);
        }

        // POST: Tags/Watch/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Watch(int id)
        {
            var tag = _context.Tags.FirstOrDefault(t => t.TagId == id);
            if (tag == null)
            {
                return NotFound();
            }

            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToAction("Login", "Account");

            // Check if user is already watching this tag
            var existingWatch = _context.UserWatchedTags
                .FirstOrDefault(w => w.UserId == userId && w.TagId == id);

            if (existingWatch == null)
            {
                // Add tag to watched tags
                var watchedTag = new UserWatchedTag
                {
                    UserId = userId,
                    TagId = id,
                    CreatedDate = DateTime.Now
                };

                _context.UserWatchedTags.Add(watchedTag);
                _context.SaveChanges();
            }

            return RedirectToAction("Index", "Home");
        }

        // POST: Tags/Unwatch/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Unwatch(int id)
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToAction("Login", "Account");

            // Find the watched tag entry
            var watchedTag = _context.UserWatchedTags
                .FirstOrDefault(w => w.UserId == userId && w.TagId == id);

            if (watchedTag != null)
            {
                // Remove tag from watched tags
                _context.UserWatchedTags.Remove(watchedTag);
                _context.SaveChanges();
            }

            return RedirectToAction("Index", "Home");
        }

        // POST: Tags/Ignore/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Ignore(int id)
        {
            var tag = _context.Tags.FirstOrDefault(t => t.TagId == id);
            if (tag == null)
            {
                return NotFound();
            }

            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToAction("Login", "Account");

            // Check if user is already ignoring this tag
            var existingIgnore = _context.UserIgnoredTags
                .FirstOrDefault(i => i.UserId == userId && i.TagId == id);

            if (existingIgnore == null)
            {
                // Add tag to ignored tags
                var ignoredTag = new UserIgnoredTag
                {
                    UserId = userId,
                    TagId = id,
                    CreatedDate = DateTime.Now
                };

                _context.UserIgnoredTags.Add(ignoredTag);
                _context.SaveChanges();
            }

            return RedirectToAction("Index", "Home");
        }

        // POST: Tags/Unignore/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Unignore(int id)
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToAction("Login", "Account");

            // Find the ignored tag entry
            var ignoredTag = _context.UserIgnoredTags
                .FirstOrDefault(i => i.UserId == userId && i.TagId == id);

            if (ignoredTag != null)
            {
                // Remove tag from ignored tags
                _context.UserIgnoredTags.Remove(ignoredTag);
                _context.SaveChanges();
            }

            return RedirectToAction("Index", "Home");
        }
        
        // GET: Tags/Search
        public IActionResult Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction(nameof(Index));
            }
            
            query = query.Trim().ToLower();
            
            // Search tags by name or description
            var tags = _context.Tags
                .Include(t => t.QuestionTags)
                .Where(t => 
                    t.TagName.ToLower().Contains(query) || 
                    (t.Description != null && t.Description.ToLower().Contains(query))
                )
                .ToList();
                
            ViewBag.SearchQuery = query;
            ViewBag.SearchResultCount = tags.Count;
            
            return View("Index", tags);
        }
    }
}