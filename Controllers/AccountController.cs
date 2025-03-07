using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using DoAnWeb.Models;
using DoAnWeb.Services;
using DoAnWeb.ViewModels;

namespace DoAnWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;

        public AccountController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Create new user
                    var user = new User
                    {
                        Username = model.Username,
                        Email = model.Email,
                        DisplayName = model.DisplayName
                    };

                    _userService.CreateUser(user, model.Password);

                    // Redirect to login page
                    TempData["SuccessMessage"] = "Registration successful. Please login.";
                    return RedirectToAction("Login");
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var user = _userService.Authenticate(model.Username, model.Password);

                if (user != null)
                {
                    // Create claims
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim("DisplayName", user.DisplayName)
                    };

                    // Create identity
                    var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");

                    // Sign in
                    await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity));

                    // Redirect to return URL or home page
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);
                    else
                        return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Invalid username or password");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Profile()
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToAction("Login");

            // Get user from database
            var user = _userService.GetUserById(userId);
            if (user == null)
                return RedirectToAction("Login");

            // Create view model
            var model = new ProfileViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Bio = user.Bio,
                AvatarUrl = user.AvatarUrl
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(ProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get current user ID from claims
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || userId != model.UserId)
                        return RedirectToAction("Login");

                    // Get user from database
                    var user = _userService.GetUserById(userId);
                    if (user == null)
                        return RedirectToAction("Login");

                    // Update user properties
                    user.DisplayName = model.DisplayName;
                    user.Email = model.Email;
                    user.Bio = model.Bio;
                    user.AvatarUrl = model.AvatarUrl;

                    // Update user
                    _userService.UpdateUser(user);

                    TempData["SuccessMessage"] = "Profile updated successfully.";
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}