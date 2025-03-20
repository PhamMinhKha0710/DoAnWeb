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

namespace DoAnWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly IGiteaUserSyncService _giteaUserSyncService;

        public AccountController(IUserService userService, IGiteaUserSyncService giteaUserSyncService)
        {
            _userService = userService;
            _giteaUserSyncService = giteaUserSyncService;
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
        public IActionResult LinkGiteaAccount()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return RedirectToAction("Login");
            }
            
            // Display the linking page
            return View();
        }
        
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkGiteaAccount(LinkGiteaViewModel model)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return RedirectToAction("Login");
            }
            
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            
            try
            {
                // Sử dụng EnsureGiteaUserAsync để liên kết tài khoản
                var result = await _giteaUserSyncService.EnsureGiteaUserAsync(userId);
                
                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"Successfully linked to Gitea with username {result.Username}";
                    return RedirectToAction("Profile");
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
        
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult UnlinkGiteaAccount()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return RedirectToAction("Login");
            }
            
            var user = _userService.GetUserById(userId);
            if (user != null)
            {
                // Xóa thông tin Gitea từ tài khoản người dùng
                user.GiteaUsername = null;
                user.GiteaToken = null;
                _userService.UpdateUser(user);
                
                TempData["SuccessMessage"] = "Successfully unlinked Gitea account";
            }
            
            return RedirectToAction("Profile");
        }
    }
}