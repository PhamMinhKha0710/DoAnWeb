using Microsoft.AspNetCore.Mvc;
using DoAnWeb.Models;
using DoAnWeb.Services;
using System.Linq;

namespace DoAnWeb.Controllers
{
    public class UsersController : Controller
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        public IActionResult Index(string filter = "reputation", string sort = "reputation", string search = null)
        {
            var users = _userService.GetAllUsers();
            
            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                users = users.Where(u => 
                    (u.Username != null && u.Username.ToLower().Contains(search)) ||
                    (u.DisplayName != null && u.DisplayName.ToLower().Contains(search)) ||
                    (u.Bio != null && u.Bio.ToLower().Contains(search))
                );
                ViewBag.SearchTerm = search;
            }
            
            // Apply filter based on user type
            switch (filter.ToLower())
            {
                case "newusers":
                    users = users.OrderByDescending(u => u.CreatedDate);
                    ViewBag.ActiveFilter = "newusers";
                    break;
                    
                case "voters":
                    users = users.OrderByDescending(u => u.Votes != null ? u.Votes.Count : 0);
                    ViewBag.ActiveFilter = "voters";
                    break;
                    
                case "editors":
                    // Assuming editors are users who have made edits to questions or answers
                    // This is a simplified example - customize based on your data model
                    users = users.OrderByDescending(u => 
                        (u.Questions != null ? u.Questions.Count(q => q.UpdatedDate > q.CreatedDate) : 0) +
                        (u.Answers != null ? u.Answers.Count(a => a.UpdatedDate > a.CreatedDate) : 0)
                    );
                    ViewBag.ActiveFilter = "editors";
                    break;
                    
                case "moderators":
                    // Assuming moderators have a specific role
                    users = users.Where(u => u.Roles != null && u.Roles.Any(r => r.RoleName == "Moderator"))
                        .OrderByDescending(u => u.Reputation);
                    ViewBag.ActiveFilter = "moderators";
                    break;
                    
                case "reputation":
                default:
                    users = users.OrderByDescending(u => u.Reputation);
                    ViewBag.ActiveFilter = "reputation";
                    break;
            }
            
            // Apply sorting within the filtered results
            switch (sort.ToLower())
            {
                case "name":
                    users = users.OrderBy(u => u.DisplayName);
                    ViewBag.ActiveSort = "name";
                    break;
                    
                case "creation":
                    users = users.OrderByDescending(u => u.CreatedDate);
                    ViewBag.ActiveSort = "creation";
                    break;
                    
                case "votes":
                    users = users.OrderByDescending(u => u.Votes != null ? u.Votes.Count : 0);
                    ViewBag.ActiveSort = "votes";
                    break;
                    
                case "reputation":
                default:
                    if (filter.ToLower() != "reputation")
                    {
                        users = users.OrderByDescending(u => u.Reputation);
                    }
                    ViewBag.ActiveSort = "reputation";
                    break;
            }
            
            // Set count of filtered results
            ViewBag.UserCount = users.Count();
            
            return View(users.ToList());
        }

        [HttpGet]
        public IActionResult SearchUsers(string query, string filter = "reputation")
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new { success = false, message = "No search query provided" });
            }
            
            var users = _userService.GetAllUsers();
            query = query.ToLower();
            
            // Filter users based on search query
            var filteredUsers = users.Where(u => 
                (u.Username != null && u.Username.ToLower().Contains(query)) ||
                (u.DisplayName != null && u.DisplayName.ToLower().Contains(query)) ||
                (u.Bio != null && u.Bio.ToLower().Contains(query))
            );
            
            // Apply filter if specified
            switch (filter.ToLower())
            {
                case "newusers":
                    filteredUsers = filteredUsers.OrderByDescending(u => u.CreatedDate);
                    break;
                    
                case "voters":
                    filteredUsers = filteredUsers.OrderByDescending(u => u.Votes != null ? u.Votes.Count : 0);
                    break;
                    
                case "editors":
                    filteredUsers = filteredUsers.OrderByDescending(u => 
                        (u.Questions != null ? u.Questions.Count(q => q.UpdatedDate > q.CreatedDate) : 0) +
                        (u.Answers != null ? u.Answers.Count(a => a.UpdatedDate > a.CreatedDate) : 0)
                    );
                    break;
                    
                case "moderators":
                    filteredUsers = filteredUsers.Where(u => u.Roles != null && u.Roles.Any(r => r.RoleName == "Moderator"))
                        .OrderByDescending(u => u.Reputation);
                    break;
                    
                case "reputation":
                default:
                    filteredUsers = filteredUsers.OrderByDescending(u => u.Reputation);
                    break;
            }
            
            // Take only top 20 results for performance
            var result = filteredUsers.Take(20).Select(u => new {
                u.UserId,
                u.DisplayName,
                u.Username,
                Avatar = string.IsNullOrEmpty(u.AvatarUrl) ? null : u.AvatarUrl,
                Reputation = u.Reputation,
                QuestionsCount = u.Questions?.Count ?? 0,
                AnswersCount = u.Answers?.Count ?? 0,
                MemberSince = u.CreatedDate?.ToString("MMM yyyy")
            }).ToList();
            
            return Json(new { 
                success = true, 
                users = result,
                totalCount = filteredUsers.Count()
            });
        }

        public IActionResult Details(int id)
        {
            var user = _userService.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        public IActionResult Profile(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Index");
            }

            var user = _userService.GetUserByUsername(username);
            if (user == null)
            {
                return NotFound();
            }

            return View("Details", user);
        }
    }
}