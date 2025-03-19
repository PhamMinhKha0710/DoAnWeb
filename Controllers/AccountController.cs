using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DoAnWeb.Models;
using DoAnWeb.Services;
using DoAnWeb.ViewModels;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

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
                    return RedirectToAction("Login", "Account", new { area = string.Empty });
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
                        new Claim("DisplayName", user.DisplayName),
                        new Claim("IsEmailVerified", user.IsEmailVerified.ToString().ToLower())
                    };

                    // Create identity
                    var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");

                    // Sign in
                    await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity));

                    // Redirect to return URL or home page
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);
                    else
                        return RedirectToAction("Index", "Home", new { area = string.Empty });
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
            return RedirectToAction("Index", "Home", new { area = string.Empty });
        }

        [HttpGet]
        public IActionResult Profile()
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToAction("Login", "Account", new { area = string.Empty });

            // Get user from database
            var user = _userService.GetUserById(userId);
            if (user == null)
                return RedirectToAction("Login", "Account", new { area = string.Empty });

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
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get current user ID from claims
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || userId != model.UserId)
                        return RedirectToAction("Login", "Account", new { area = string.Empty });

                    // Get user from database
                    var user = _userService.GetUserById(userId);
                    if (user == null)
                        return RedirectToAction("Login", "Account", new { area = string.Empty });

                    // Update user properties
                    user.DisplayName = model.DisplayName;
                    user.Email = model.Email;
                    user.Bio = model.Bio;

                    Console.WriteLine($"Profile update - User ID: {userId}");
                    Console.WriteLine($"RemoveAvatar: {model.RemoveAvatar}");
                    Console.WriteLine($"Has profile image: {model.ProfileImage != null}");
                    if (model.ProfileImage != null)
                    {
                        Console.WriteLine($"File name: {model.ProfileImage.FileName}, Size: {model.ProfileImage.Length} bytes");
                    }
                    Console.WriteLine($"AvatarUrl: {model.AvatarUrl}");

                    // Handle profile image upload or removal
                    if (model.ProfileImage != null && model.ProfileImage.Length > 0)
                    {
                        // Process the uploaded file
                        string avatarUrl = await ProcessProfileImageUpload(model.ProfileImage, userId);
                        Console.WriteLine($"Image processed successfully. New URL: {avatarUrl}");
                        user.AvatarUrl = avatarUrl;
                    }
                    else if (model.RemoveAvatar)
                    {
                        // Remove the avatar
                        if (!string.IsNullOrEmpty(user.AvatarUrl))
                        {
                            string oldImagePath = user.AvatarUrl.Replace("/uploads/profiles/", "");
                            string oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles", oldImagePath);
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                try
                                {
                                    System.IO.File.Delete(oldFilePath);
                                    Console.WriteLine($"Deleted old avatar file: {oldFilePath}");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error deleting old avatar file: {ex.Message}");
                                }
                            }
                        }
                        user.AvatarUrl = string.Empty;
                        Console.WriteLine("Avatar removed");
                    }
                    else if (!string.IsNullOrWhiteSpace(model.AvatarUrl))
                    {
                        // Use provided URL
                        user.AvatarUrl = model.AvatarUrl;
                        Console.WriteLine($"Using provided avatar URL: {model.AvatarUrl}");
                    }

                    // Update user
                    _userService.UpdateUser(user);
                    Console.WriteLine("User updated successfully");

                    TempData["SuccessMessage"] = "Profile updated successfully.";
                    return RedirectToAction("Profile");
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                    Console.WriteLine($"Argument exception: {ex.Message}");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while updating your profile. Please try again.");
                    // Log the exception
                    Console.WriteLine($"Exception updating profile: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
            else
            {
                Console.WriteLine("Model state is invalid");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation error: {error.ErrorMessage}");
                }
            }

            return View(model);
        }

        private async Task<string> ProcessProfileImageUpload(IFormFile profileImage, int userId)
        {
            try
            {
                Console.WriteLine("---Starting ProcessProfileImageUpload---");
                
                if (profileImage == null)
                {
                    Console.WriteLine("ERROR: ProfileImage is null");
                    throw new ArgumentException("The file is empty or not provided.");
                }
                
                if (profileImage.Length == 0)
                {
                    Console.WriteLine("ERROR: ProfileImage length is 0");
                    throw new ArgumentException("The file is empty.");
                }

                Console.WriteLine($"File info - Name: {profileImage.FileName}, Length: {profileImage.Length}, ContentType: {profileImage.ContentType}");

                // Validate file size (5MB limit)
                if (profileImage.Length > 5 * 1024 * 1024)
                {
                    Console.WriteLine("ERROR: File size exceeds 5MB");
                    throw new ArgumentException("File size exceeds the 5MB limit.");
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(profileImage.FileName).ToLowerInvariant();
                
                Console.WriteLine($"File extension: {extension}");
                
                if (!allowedExtensions.Contains(extension))
                {
                    Console.WriteLine($"ERROR: Invalid file extension: {extension}");
                    throw new ArgumentException("Only JPG, PNG, and GIF files are allowed.");
                }

                // Create directories
                var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                Console.WriteLine($"wwwrootPath: {wwwrootPath}");
                Console.WriteLine($"Directory exists: {Directory.Exists(wwwrootPath)}");
                
                var uploadsFolder = Path.Combine(wwwrootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Console.WriteLine($"Creating directory: {uploadsFolder}");
                    Directory.CreateDirectory(uploadsFolder);
                }
                else
                {
                    Console.WriteLine($"Directory already exists: {uploadsFolder}");
                }
                
                var profilesFolder = Path.Combine(uploadsFolder, "profiles");
                if (!Directory.Exists(profilesFolder))
                {
                    Console.WriteLine($"Creating directory: {profilesFolder}");
                    Directory.CreateDirectory(profilesFolder);
                }
                else
                {
                    Console.WriteLine($"Directory already exists: {profilesFolder}");
                }

                // Generate unique filename
                var uniqueFileName = $"{userId}_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(profilesFolder, uniqueFileName);
                Console.WriteLine($"Will save file to: {filePath}");

                // Save the file
                try 
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        Console.WriteLine("Copying file to stream...");
                        await profileImage.CopyToAsync(fileStream);
                        Console.WriteLine("File saved successfully");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR saving file: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    throw new Exception($"Error saving file: {ex.Message}", ex);
                }

                // Delete old profile image if it exists
                try
                {
                    var user = _userService.GetUserById(userId);
                    if (user != null && !string.IsNullOrEmpty(user.AvatarUrl))
                    {
                        Console.WriteLine($"Current avatar URL: {user.AvatarUrl}");
                        if (user.AvatarUrl.Contains("/uploads/profiles/"))
                        {
                            var oldImagePath = user.AvatarUrl.Replace("/uploads/profiles/", "");
                            var oldFilePath = Path.Combine(profilesFolder, oldImagePath);
                            Console.WriteLine($"Checking for old file: {oldFilePath}");
                            
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                Console.WriteLine($"Deleting old file: {oldFilePath}");
                                System.IO.File.Delete(oldFilePath);
                                Console.WriteLine("Old file deleted successfully");
                            }
                            else
                            {
                                Console.WriteLine("Old file not found, nothing to delete");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Current avatar is external URL, nothing to delete");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No existing avatar URL");
                    }
                }
                catch (Exception ex)
                {
                    // Just log the error but continue, as this is just cleanup
                    Console.WriteLine($"WARNING: Error cleaning up old profile image: {ex.Message}");
                }

                // Return the relative URL
                var avatarUrl = $"/uploads/profiles/{uniqueFileName}";
                Console.WriteLine($"Returning avatar URL: {avatarUrl}");
                Console.WriteLine("---ProcessProfileImageUpload completed successfully---");
                return avatarUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CRITICAL ERROR in ProcessProfileImageUpload: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw new Exception($"Error uploading file: {ex.Message}", ex);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                TempData["ErrorMessage"] = "All password fields are required.";
                return RedirectToAction("Profile");
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "New password and confirmation password do not match.";
                return RedirectToAction("Profile");
            }

            try
            {
                // Get current user ID from claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                    return RedirectToAction("Login");

                // Change password
                bool success = _userService.ChangePassword(userId, currentPassword, newPassword);
                if (success)
                {
                    TempData["SuccessMessage"] = "Password changed successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Current password is incorrect.";
                }
            }
            catch (InvalidOperationException ex)
            {
                // Handle account verification errors
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (ArgumentException ex)
            {
                // Handle password validation errors
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAccount(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                TempData["ErrorMessage"] = "Password is required to delete your account.";
                return RedirectToAction("Profile");
            }

            try
            {
                // Get current user ID from claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                    return RedirectToAction("Login");

                // Verify password and delete account
                bool success = _userService.DeleteAccount(userId, password);
                if (success)
                {
                    // Sign out the user
                    HttpContext.SignOutAsync();
                    TempData["SuccessMessage"] = "Your account has been successfully deleted.";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["ErrorMessage"] = "Incorrect password. Account deletion failed.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Profile");
        }

        [HttpGet]
        [Authorize]
        public IActionResult Saves()
        {
            return RedirectToAction("Index", "SavedItems");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult VerifyEmail(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Invalid verification token.";
                return RedirectToAction("Profile");
            }

            try
            {
                bool result = _userService.VerifyEmail(token);
                if (result)
                {
                    // Update the user's claims to reflect the verified email
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                    {
                        var user = _userService.GetUserById(userId);
                        if (user != null)
                        {
                            // Create updated claims
                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, user.Username),
                                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                                new Claim("DisplayName", user.DisplayName),
                                new Claim("IsEmailVerified", "true")
                            };

                            // Create identity and sign in again to update claims
                            var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
                            HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity));
                        }
                    }

                    TempData["SuccessMessage"] = "Your email has been successfully verified.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Email verification failed. The token may be expired or invalid.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred during email verification: {ex.Message}";
            }

            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResendVerificationEmail()
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToAction("Login");

            try
            {
                bool result = _userService.SendVerificationEmail(userId);
                if (result)
                {
                    TempData["SuccessMessage"] = "Verification email has been sent. Please check your inbox.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to send verification email. Please try again later.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Profile");
        }
    }
}