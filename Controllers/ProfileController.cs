using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DoAnWeb.Services;

namespace DoAnWeb.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IUserService _userService;

        public ProfileController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: /Profile
        public IActionResult Index()
        {
            // Chuyển hướng đến trang profile trong AccountController 
            // (tái sử dụng logic hiện có thay vì lặp lại code)
            return RedirectToAction("Profile", "Account");
        }

        // GET: /Profile/{username}
        [AllowAnonymous]
        public IActionResult ViewProfile(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Index", "Home");
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