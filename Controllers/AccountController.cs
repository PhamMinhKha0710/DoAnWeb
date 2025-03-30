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
using DoAnWeb.GitIntegration;
using System.Linq;
using DoAnWeb.Repositories;
using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace DoAnWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly IGiteaUserSyncService _giteaUserSyncService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IUserService userService, IGiteaUserSyncService giteaUserSyncService, ILogger<AccountController> logger)
        {
            _userService = userService;
            _giteaUserSyncService = giteaUserSyncService;
            _logger = logger;
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
                    // Cập nhật thời gian đăng nhập cuối cùng của người dùng
                    user.LastLoginDate = DateTime.Now;
                    _userService.UpdateUser(user);

                    // Tạo các claims cho xác thực
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim("DisplayName", user.DisplayName)
                    };

                    // Add Gitea information if available
                    if (!string.IsNullOrEmpty(user.GiteaUsername))
                    {
                        claims.Add(new Claim("GiteaUsername", user.GiteaUsername));
                    }
                    
                    // Verify email status
                    claims.Add(new Claim("IsEmailVerified", user.IsEmailVerified.ToString()));

                    // Thêm claim cho các vai trò (roles)
                    if (user.Roles != null)
                    {
                        foreach (var role in user.Roles)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, role.RoleName));
                        }
                    }

                    // Đặt thời gian hiệu lực cho cookie
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(model.RememberMe ? 30 : 1)
                    };

                    // Tạo cookie xác thực
                    var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
                    await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

                    // Lưu thông tin người dùng vào Session để hiển thị thông báo nâng cấp bảo mật
                    HttpContext.Session.SetString("LastLogin", DateTime.Now.ToString());
                    HttpContext.Session.SetString("SecurityUpgrade", user.HashType);

                    // Nếu returnUrl không hợp lệ, chuyển hướng đến trang chủ
                    if (!Url.IsLocalUrl(returnUrl))
                    {
                        return RedirectToAction(nameof(HomeController.Index), "Home");
                    }
                    
                    return Redirect(returnUrl);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác.");
                }
            }

            // If we got this far, something failed, redisplay form
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
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return RedirectToAction("Login");
            }

            var profileViewModel = _userService.GetUserProfile(userId);
            if (profileViewModel == null)
            {
                return RedirectToAction("Login");
            }

            return View(profileViewModel);
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

                // Get application base path
                var basePath = Directory.GetCurrentDirectory();
                Console.WriteLine($"Application base path: {basePath}");
                
                // Create directories
                var wwwrootPath = Path.Combine(basePath, "wwwroot");
                Console.WriteLine($"wwwrootPath: {wwwrootPath}");
                
                // Verify wwwroot exists
                if (!Directory.Exists(wwwrootPath))
                {
                    Console.WriteLine($"ERROR: wwwroot directory not found at: {wwwrootPath}");
                    throw new DirectoryNotFoundException($"wwwroot directory not found: {wwwrootPath}");
                }
                
                var uploadsFolder = Path.Combine(wwwrootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Console.WriteLine($"Creating directory: {uploadsFolder}");
                    Directory.CreateDirectory(uploadsFolder);
                }
                
                var profilesFolder = Path.Combine(uploadsFolder, "profiles");
                if (!Directory.Exists(profilesFolder))
                {
                    Console.WriteLine($"Creating directory: {profilesFolder}");
                    Directory.CreateDirectory(profilesFolder);
                }

                // Generate unique filename with timestamp to avoid caching issues
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var uniqueFileName = $"{userId}_{timestamp}_{Guid.NewGuid().ToString().Substring(0, 8)}{extension}";
                var filePath = Path.Combine(profilesFolder, uniqueFileName);
                Console.WriteLine($"Will save file to: {filePath}");

                // Save the file
                try 
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        Console.WriteLine("Copying file to stream...");
                        await profileImage.CopyToAsync(fileStream);
                        await fileStream.FlushAsync();
                        Console.WriteLine("File saved successfully");
                    }
                    
                    // Verify file was created
                    if (!System.IO.File.Exists(filePath))
                    {
                        Console.WriteLine($"ERROR: File was not created at path: {filePath}");
                        throw new IOException($"Failed to create file at: {filePath}");
                    }
                    else
                    {
                        Console.WriteLine($"File exists check: {System.IO.File.Exists(filePath)}");
                        Console.WriteLine($"File size: {new FileInfo(filePath).Length} bytes");
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
                            var oldImageName = Path.GetFileName(user.AvatarUrl);
                            var oldFilePath = Path.Combine(profilesFolder, oldImageName);
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

                // Return the relative URL with a timestamp query parameter to prevent browser caching
                var avatarUrl = $"/uploads/profiles/{uniqueFileName}?t={timestamp}";
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
        [Authorize]
        public IActionResult TestSavedItems([FromServices] IUserSavedItemRepository savedItemRepository)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToAction("Login");

            // Get all saved items for current user
            var savedItems = savedItemRepository.GetSavedItemsByUserId(userId);
            
            // Check if user has any saved items
            if (!savedItems.Any())
            {
                // For testing, add a dummy saved item
                savedItemRepository.SaveItem(userId, "Question", 1); // Assuming question with ID 1 exists
                TempData["SuccessMessage"] = "Test item saved successfully!";
            }
            else
            {
                TempData["InfoMessage"] = $"You already have {savedItems.Count()} saved items.";
            }
            
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

        [Authorize]
        public async Task<IActionResult> LinkGiteaAccount()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return RedirectToAction("Login");
            }
            
            // Check if user already has a linked account
            var user = _userService.GetUserById(userId);
            if (user != null && !string.IsNullOrEmpty(user.GiteaUsername) && !string.IsNullOrEmpty(user.GiteaToken))
            {
                // Already linked, redirect to Gitea login
                return RedirectToAction("GiteaLogin");
            }
            
            // Display the linking page
            return View();
        }
        
        [Authorize]
        public async Task<IActionResult> GiteaLogin()
        {
            try
            {
                // Get current user ID
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                if (userId == 0)
                {
                    TempData["ErrorMessage"] = "You need to be logged in to access Gitea.";
                    return RedirectToAction("Login");
                }
                
                // Get user from database
                var user = _userService.GetUserById(userId);
                
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Login");
                }
                
                // Check if user has Gitea account linked
                if (string.IsNullOrEmpty(user.GiteaUsername) || string.IsNullOrEmpty(user.GiteaToken))
                {
                    TempData["ErrorMessage"] = "You need to link your Gitea account first.";
                    return RedirectToAction("LinkGiteaAccount");
                }
                
                // Try to get Gitea login URL
                try
                {
                    // Get Gitea login URL from the service
                    var giteaLoginUrl = await _giteaUserSyncService.GetGiteaLoginUrlAsync(userId);
                    
                    if (string.IsNullOrEmpty(giteaLoginUrl))
                    {
                        // Try to refresh the token
                        _logger.LogWarning($"Failed to generate login URL for user {userId}, attempting to refresh Gitea account");
                        var userResult = await _giteaUserSyncService.EnsureGiteaUserWithDetailsAsync(userId);
                        
                        if (!userResult.Success)
                        {
                            TempData["ErrorMessage"] = $"Failed to access Gitea: {userResult.ErrorMessage}";
                            return RedirectToAction("LinkGiteaAccount");
                        }
                        
                        giteaLoginUrl = await _giteaUserSyncService.GetGiteaLoginUrlAsync(userId);
                        
                        if (string.IsNullOrEmpty(giteaLoginUrl))
                        {
                            TempData["ErrorMessage"] = "Failed to generate Gitea login URL. Please try again later.";
                            return RedirectToAction("Profile");
                        }
                    }
                    
                    // If it's already a full URL, use it directly
                    if (giteaLoginUrl.StartsWith("http"))
                    {
                        return Redirect(giteaLoginUrl);
                    }
                    
                    // Otherwise, it's a session ID, so construct the URL
                    return Redirect($"http://localhost:3000?_session={giteaLoginUrl}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error logging in to Gitea for user {userId}: {ex.Message}");
                    TempData["ErrorMessage"] = $"Error logging in to Gitea: {ex.Message}";
                    return RedirectToAction("Profile");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Profile");
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkGiteaAccount(LinkGiteaViewModel model)
        {
            try
            {
                // Get current user ID
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    TempData["ErrorMessage"] = "You need to be logged in.";
                    return RedirectToAction("Login");
                }
                
                var userId = userIdClaim.Value;
                
                // Check if we need to create a new account automatically
                if (model.CreateNewAccount)
                {
                    try
                    {
                        // Use EnsureGiteaUserAsync which will create an account
                        var loginUrl = await _giteaUserSyncService.EnsureGiteaUserAsync(userId);
                        
                        if (!string.IsNullOrEmpty(loginUrl))
                        {
                            TempData["SuccessMessage"] = "Successfully created and linked Gitea account";
                            
                            // Immediately redirect to Gitea with auto-login
                            return RedirectToAction("GiteaLogin");
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Failed to create Gitea account. Please try linking to an existing account.";
                            model.CreateNewAccount = false;
                            return View(model);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error creating Gitea account: {ex.Message}");
                        ModelState.AddModelError("", $"Error creating account: {ex.Message}");
                        model.CreateNewAccount = false;
                        return View(model);
                    }
                }
                
                // If we get here, we're linking to an existing account
                // Manual linking with provided credentials
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                
                if (string.IsNullOrEmpty(model.GiteaUsername) || string.IsNullOrEmpty(model.GiteaPassword))
                {
                    ModelState.AddModelError("", "Username and password are required for linking to an existing account");
                    return View(model);
                }
                
                var result = await _giteaUserSyncService.LinkGiteaAccountAsync(
                    int.Parse(userId), 
                    model.GiteaUsername, 
                    model.GiteaPassword);
                
                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"Successfully linked to Gitea account {result.Username}";
                    return RedirectToAction("GiteaLogin");
                }
                else
                {
                    ModelState.AddModelError("", $"Failed to link Gitea account: {result.ErrorMessage}");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error: {ex.Message}");
                return View(model);
            }
        }
        
        // New method to automatically create and link a Gitea account
        [Authorize]
        public async Task<IActionResult> AutoLinkGiteaAccount()
        {
            try
            {
                // Get current user ID
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    TempData["ErrorMessage"] = "You need to be logged in to access Gitea.";
                    return RedirectToAction("Login");
                }
                
                var userId = int.Parse(userIdClaim.Value);
                
                // Automatically create and link a Gitea account
                try
                {
                    var userResult = await _giteaUserSyncService.EnsureGiteaUserWithDetailsAsync(userId);
                    
                    if (userResult.Success)
                    {
                        TempData["SuccessMessage"] = "Successfully created and linked Gitea account";
                        return RedirectToAction("GiteaLogin");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"Failed to create Gitea account: {userResult.ErrorMessage}";
                        return RedirectToAction("LinkGiteaAccount");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error creating Gitea account for user {userId}: {ex.Message}");
                    TempData["ErrorMessage"] = $"Failed to create Gitea account: {ex.Message}";
                    return RedirectToAction("LinkGiteaAccount");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Profile");
            }
        }
        
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlinkGiteaAccount()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return RedirectToAction("Login");
            }
            
            bool result = await _giteaUserSyncService.UnlinkGiteaAccountAsync(userId);
            
            if (result)
            {
                TempData["SuccessMessage"] = "Successfully unlinked Gitea account";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to unlink Gitea account";
            }
            
            return RedirectToAction("Profile");
        }

        // Methods for Google and GitHub authentication

        [HttpGet]
        public IActionResult ExternalLogin(string provider, string returnUrl = null, string isRegistration = null)
        {
            // Request a redirect to the external login provider
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl, isRegistration = isRegistration });
            var properties = new AuthenticationProperties 
            { 
                RedirectUri = redirectUrl,
                Items = { 
                    ["returnUrl"] = returnUrl,
                    ["isRegistration"] = isRegistration ?? "false"
                },
                AllowRefresh = true
            };
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null, string isRegistration = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            bool isRegistrationFlow = !string.IsNullOrEmpty(isRegistration) && isRegistration.ToLower() == "true";

            if (remoteError != null)
            {
                TempData["ErrorMessage"] = $"Error from external provider: {remoteError}";
                return isRegistrationFlow
                    ? RedirectToAction(nameof(Register))
                    : RedirectToAction(nameof(Login), new { ReturnUrl = returnUrl });
            }

            // Debug: Log start of authentication process
            Console.WriteLine("--- Starting external authentication callback ---");
            Console.WriteLine($"Is registration flow: {isRegistrationFlow}");

            // Get the login information from the external provider
            var info = await HttpContext.AuthenticateAsync("Google");
            if (info == null || info.Principal == null)
            {
                // Try GitHub
                Console.WriteLine("Google authentication failed or not found, trying GitHub...");
                info = await HttpContext.AuthenticateAsync("GitHub");
            }
            
            if (info?.Principal == null)
            {
                Console.WriteLine("ERROR: Failed to authenticate with any external provider");
                TempData["ErrorMessage"] = "Error loading external login information.";
                return isRegistrationFlow
                    ? RedirectToAction(nameof(Register))
                    : RedirectToAction(nameof(Login), new { ReturnUrl = returnUrl });
            }

            // Debug: Log authentication details
            Console.WriteLine($"Successfully authenticated with provider: {info.Principal.Identity?.AuthenticationType}");
            Console.WriteLine("Claims received from provider:");
            foreach (var claim in info.Principal.Claims)
            {
                Console.WriteLine($"  {claim.Type} = {claim.Value}");
            }

            // Get information from the external login provider
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var name = info.Principal.FindFirstValue(ClaimTypes.Name);
            var providerId = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var provider = info.Properties.Items.ContainsKey("LoginProvider") 
                ? info.Properties.Items["LoginProvider"] 
                : (info.Principal.Identity?.AuthenticationType ?? "External");

            // GitHub might not include email in standard claims, check additional claims
            if (string.IsNullOrEmpty(email) && provider == "GitHub")
            {
                // Try to find email in GitHub-specific claims
                email = info.Principal.FindFirstValue("urn:github:email");
                
                if (string.IsNullOrEmpty(email))
                {
                    // Look through all claims to find email
                    foreach (var claim in info.Principal.Claims)
                    {
                        if (claim.Type.EndsWith("emailaddress", StringComparison.OrdinalIgnoreCase) ||
                            claim.Type.Contains("email", StringComparison.OrdinalIgnoreCase))
                        {
                            email = claim.Value;
                            break;
                        }
                        
                        // Debug: Log all claims to see what's available
                        Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
                    }
                }
                
                // Last resort: Create a placeholder email using GitHub username
                if (string.IsNullOrEmpty(email))
                {
                    var githubUsername = info.Principal.FindFirstValue("urn:github:login") ?? 
                                        info.Principal.FindFirstValue("login") ??
                                        providerId;
                    
                    if (!string.IsNullOrEmpty(githubUsername))
                    {
                        Console.WriteLine($"Creating placeholder email for GitHub user: {githubUsername}");
                        email = $"{githubUsername}@users.noreply.github.com";
                    }
                }
            }

            if (string.IsNullOrEmpty(email))
            {
                Console.WriteLine("ERROR: Unable to retrieve email from provider claims");
                TempData["ErrorMessage"] = "Unable to get email from external provider.";
                return isRegistrationFlow
                    ? RedirectToAction(nameof(Register))
                    : RedirectToAction(nameof(Login), new { ReturnUrl = returnUrl });
            }

            // Look for existing user by email
            var user = _userService.GetUserByEmail(email);

            // Check if this is a registration attempt for an existing email
            if (isRegistrationFlow && user != null)
            {
                Console.WriteLine($"Registration attempt with existing email: {email}");
                TempData["ErrorMessage"] = $"An account with the email {email} already exists. Please use the login page instead.";
                return RedirectToAction(nameof(Register));
            }

            // If user doesn't exist, create new user
            if (user == null)
            {
                // Generate username based on email
                var username = email.Split('@')[0];
                var baseUsername = username;
                int counter = 1;

                // Ensure username is unique
                while (_userService.GetUserByUsername(username) != null)
                {
                    username = $"{baseUsername}{counter++}";
                }

                // Try to get avatar URL from Google or GitHub claims
                string avatarUrl = null;
                
                // Google uses "picture" claim
                if (provider == "Google")
                {
                    avatarUrl = info.Principal.FindFirstValue("picture");
                }
                // GitHub uses various claims, but we'll check some common ones
                else if (provider == "GitHub")
                {
                    avatarUrl = info.Principal.FindFirstValue("urn:github:avatar_url") 
                        ?? info.Principal.FindFirstValue("avatar_url");
                }

                user = new User
                {
                    Username = username,
                    Email = email,
                    DisplayName = name ?? username,
                    IsEmailVerified = true, // Email is already verified through the provider
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now,
                    AvatarUrl = avatarUrl, // Set avatar URL if available
                    // Generate a random password since they won't use it
                    PasswordHash = Guid.NewGuid().ToString("N")
                };

                try
                {
                    _userService.CreateExternalUser(user);
                    Console.WriteLine($"Created new user via external provider: {email}");
                    
                    if (isRegistrationFlow)
                    {
                        TempData["SuccessMessage"] = "Your account has been successfully created! You may now log in.";
                        return RedirectToAction(nameof(Login));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating user: {ex.Message}");
                    TempData["ErrorMessage"] = $"Error creating account: {ex.Message}";
                    return isRegistrationFlow
                        ? RedirectToAction(nameof(Register))
                        : RedirectToAction(nameof(Login), new { ReturnUrl = returnUrl });
                }
            }
            else if (isRegistrationFlow)
            {
                // Shouldn't reach here because of the earlier check, but just in case
                TempData["ErrorMessage"] = $"An account with the email {email} already exists. Please use the login page instead.";
                return RedirectToAction(nameof(Register));
            }

            // Update user's last login date
            user.LastLoginDate = DateTime.Now;
            _userService.UpdateUser(user);

            // Create claims for authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("DisplayName", user.DisplayName)
            };

            // Add role claims
            if (user.Roles != null)
            {
                foreach (var role in user.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role.RoleName));
                }
            }

            // Add provider information
            claims.Add(new Claim("ExternalProvider", provider));

            // Set cookie properties
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            };

            // Create the authentication cookie
            var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
            await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

            return RedirectToLocal(returnUrl);
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Generate reset token (returns null if email doesn't exist)
                var token = _userService.GeneratePasswordResetToken(model.Email);
                
                if (token != null)
                {
                    // Build reset link for email
                    var resetUrl = Url.Action("ResetPassword", "Account", 
                        new { email = model.Email, token = token }, 
                        protocol: HttpContext.Request.Scheme);
                    
                    // Log the reset URL for debugging (in a real app, this would be sent via email)
                    Console.WriteLine($"Password reset link generated: {resetUrl}");
                    Console.WriteLine($"This should be emailed to: {model.Email}");
                    
                    // Display a generic success message to prevent user enumeration
                    TempData["SuccessMessage"] = "If your email exists in our system, you will receive a password reset link shortly.";
                    
                    // TODO: In a production application, this is where you would send an email
                    // For demonstration purposes, we'll just return a success message
                    
                    // Display the reset link in the success message (ONLY FOR DEMONSTRATION)
                    // In a real application, you would NOT show this to the user but send it via email
                    TempData["SuccessMessage"] = $"For demonstration purposes, here is your reset link: <a href='{resetUrl}'>Reset Password</a>";
                    
                    return View("ForgotPasswordConfirmation");
                }
                
                // Display a generic success message even if email doesn't exist to prevent user enumeration
                TempData["SuccessMessage"] = "If your email exists in our system, you will receive a password reset link shortly.";
                return View("ForgotPasswordConfirmation");
            }
            
            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Invalid password reset link.";
                return RedirectToAction("Login");
            }
            
            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            };
            
            // Validate token
            if (!_userService.ValidatePasswordResetToken(email, token))
            {
                TempData["ErrorMessage"] = "The password reset link has expired or is invalid.";
                return RedirectToAction("Login");
            }
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            
            // Reset password
            var result = _userService.ResetPassword(model.Email, model.Token, model.Password);
            
            if (result)
            {
                Console.WriteLine($"Password successfully reset for user: {model.Email}");
                return RedirectToAction("ResetPasswordConfirmation");
            }
            
            // If we got this far, reset failed
            TempData["ErrorMessage"] = "Password reset failed. The link may have expired or is invalid.";
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
    }
}