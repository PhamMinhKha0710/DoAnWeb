using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DoAnWeb.Models;
using DoAnWeb.Services;

namespace DoAnWeb.GitIntegration
{
    public class GiteaUserSyncService : IGiteaUserSyncService
    {
        private readonly IGiteaIntegrationService _giteaService;
        private readonly ILogger<GiteaUserSyncService> _logger;
        private readonly IUserService _userService;

        public GiteaUserSyncService(
            IGiteaIntegrationService giteaService,
            IUserService userService,
            ILogger<GiteaUserSyncService> logger)
        {
            _giteaService = giteaService;
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Đảm bảo người dùng trong DevCommunity có tương ứng trong Gitea
        /// </summary>
        public async Task<GiteaUserResult> EnsureGiteaUserAsync(int userId)
        {
            try
            {
                // Lấy thông tin người dùng từ DevCommunity
                var user = _userService.GetUserById(userId);
                
                if (user == null)
                {
                    _logger.LogError($"User with ID {userId} not found");
                    return new GiteaUserResult
                    {
                        Success = false,
                        ErrorMessage = $"User with ID {userId} not found"
                    };
                }
                
                // Nếu người dùng đã có thông tin Gitea, trả về thông tin đó
                if (!string.IsNullOrEmpty(user.GiteaUsername) && !string.IsNullOrEmpty(user.GiteaToken))
                {
                    return new GiteaUserResult
                    {
                        Success = true,
                        Username = user.GiteaUsername,
                        AccessToken = user.GiteaToken
                    };
                }
                
                // Tạo người dùng mới trong Gitea
                string giteaUsername = GenerateGiteaUsername(user.Username);
                string giteaPassword = GenerateSecurePassword();
                bool created = await _giteaService.CreateUserAsync(
                    giteaUsername,
                    user.Email,
                    giteaPassword,
                    user.DisplayName);
                
                if (!created)
                {
                    _logger.LogError($"Failed to create Gitea user for {user.Username}");
                    return new GiteaUserResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to create Gitea user"
                    };
                }
                
                // Tạo token truy cập cho người dùng
                string accessToken = await _giteaService.CreateAccessTokenAsync(
                    giteaUsername,
                    giteaPassword,
                    "DevCommunity API Token");
                
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogError($"Failed to create access token for Gitea user {giteaUsername}");
                    return new GiteaUserResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to create access token"
                    };
                }
                
                // Cập nhật thông tin Gitea trong hồ sơ người dùng
                user.GiteaUsername = giteaUsername;
                user.GiteaToken = accessToken;
                _userService.UpdateUser(user);
                
                return new GiteaUserResult
                {
                    Success = true,
                    Username = giteaUsername,
                    AccessToken = accessToken
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error ensuring Gitea user for user ID {userId}");
                return new GiteaUserResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Tạo tên người dùng Gitea dựa trên tên người dùng DevCommunity
        /// </summary>
        private string GenerateGiteaUsername(string username)
        {
            // Đảm bảo tên người dùng không có ký tự đặc biệt và độ dài phù hợp
            string sanitized = username.Replace(" ", "").Replace("@", "").Replace(".", "_");
            
            if (sanitized.Length > 20)
            {
                sanitized = sanitized.Substring(0, 20);
            }
            
            return sanitized;
        }

        /// <summary>
        /// Tạo mật khẩu an toàn ngẫu nhiên
        /// </summary>
        private string GenerateSecurePassword()
        {
            // Tạo mật khẩu ngẫu nhiên 16 ký tự bao gồm chữ cái, số và ký tự đặc biệt
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 16)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }

    public class GiteaUserResult
    {
        public bool Success { get; set; }
        public string Username { get; set; }
        public string AccessToken { get; set; }
        public string ErrorMessage { get; set; }
    }

    public interface IGiteaUserSyncService
    {
        Task<GiteaUserResult> EnsureGiteaUserAsync(int userId);
    }
} 