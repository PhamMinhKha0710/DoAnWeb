using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DoAnWeb.Models;
using DoAnWeb.Services;
using System.Net.Http;

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
        /// Đảm bảo rằng người dùng có tài khoản Gitea và được liên kết đúng
        /// Ensures that the user has a Gitea account and is properly linked.
        /// </summary>
        public async Task<string> EnsureGiteaUserAsync(string userId)
        {
            try
            {
                _logger.LogInformation($"Ensuring Gitea user for User ID: {userId}");
                
                // First check if Gitea server is accessible
                var isServerAccessible = await _giteaService.TestGiteaServerConnectivityAsync();
                if (!isServerAccessible)
                {
                    _logger.LogError("Cannot ensure Gitea user because Gitea server is not accessible");
                    throw new Exception("Gitea server is not accessible. Please check server configuration and connection.");
                }
                
                // Get user from DevCommunity
                var user = _userService.GetUserById(int.Parse(userId));
                if (user == null)
                {
                    _logger.LogError($"User with ID {userId} not found in DevCommunity");
                    throw new Exception($"User with ID {userId} not found");
                }
                
                _logger.LogInformation($"Found user {user.Username} with email {user.Email}");
                
                // Check if user already has a Gitea account
                if (!string.IsNullOrEmpty(user.GiteaUsername) && !string.IsNullOrEmpty(user.GiteaToken))
                {
                    _logger.LogInformation($"User {user.Username} already has a Gitea account with username {user.GiteaUsername}");
                    return await _giteaService.GenerateLoginSessionAsync(user.GiteaUsername, user.GiteaToken);
                }
                
                _logger.LogInformation($"Creating new Gitea account for user {user.Username}");
                
                // Create username for Gitea - sanitize to avoid invalid characters
                string giteaUsername = GenerateGiteaUsername(user.Username);
                
                // Try to create a new Gitea user
                var password = GenerateSecurePassword();
                _logger.LogInformation($"Attempting to create Gitea user with username: {giteaUsername}");
                
                var userCreated = await _giteaService.CreateUserAsync(giteaUsername, user.Email, password, user.DisplayName ?? user.Username);
                
                if (!userCreated)
                {
                    _logger.LogWarning($"Failed to create Gitea user with username {giteaUsername}. Trying alternative username...");
                    
                    // Try with alternative username if the first attempt failed
                    giteaUsername = $"{giteaUsername}_{Guid.NewGuid().ToString().Substring(0, 8)}";
                    _logger.LogInformation($"Attempting with alternative username: {giteaUsername}");
                    
                    userCreated = await _giteaService.CreateUserAsync(giteaUsername, user.Email, password, user.DisplayName ?? user.Username);
                    
                    if (!userCreated)
                    {
                        _logger.LogError($"Failed to create Gitea user with alternative username {giteaUsername}");
                        throw new Exception("Failed to create Gitea user. Please try again later or contact support.");
                    }
                }
                
                _logger.LogInformation($"Successfully created Gitea user {giteaUsername}");
                
                // Create access token for the user
                _logger.LogInformation($"Creating access token for Gitea user {giteaUsername}");
                var accessToken = await _giteaService.CreateAccessTokenAsync(giteaUsername, password, "DevCommunity Integration");
                
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogError($"Failed to create access token for Gitea user {giteaUsername}");
                    throw new Exception("Failed to create access token for Gitea user");
                }
                
                _logger.LogInformation($"Successfully created access token for Gitea user {giteaUsername}");
                
                // Update user profile with Gitea info
                user.GiteaUsername = giteaUsername;
                user.GiteaToken = accessToken;
                
                try
                {
                    _userService.UpdateUser(user);
                    _logger.LogInformation($"Successfully updated user profile with Gitea username {giteaUsername}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to update user profile with Gitea info: {ex.Message}");
                    throw new Exception($"Failed to link Gitea account: {ex.Message}", ex);
                }
                
                // Generate login session
                return await _giteaService.GenerateLoginSessionAsync(giteaUsername, accessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in EnsureGiteaUserAsync for user ID {userId}: {ex.Message}");
                throw new Exception($"Failed to create Gitea account: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Ensures that the user has a Gitea account and is properly linked (overload accepting int userId)
        /// </summary>
        public async Task<string> EnsureGiteaUserAsync(int userId)
        {
            return await EnsureGiteaUserAsync(userId.ToString());
        }
        
        /// <summary>
        /// Get or create a Gitea user and return a GiteaUserResult with additional info
        /// </summary>
        public async Task<GiteaUserResult> EnsureGiteaUserWithDetailsAsync(int userId)
        {
            try
            {
                _logger.LogInformation($"Ensuring Gitea user for ID {userId}");
                var user = _userService.GetUserById(userId);
                
                if (user == null)
                {
                    _logger.LogError($"User with ID {userId} not found in database");
                    return new GiteaUserResult
                    {
                        Success = false,
                        ErrorMessage = $"User with ID {userId} not found"
                    };
                }

                _logger.LogInformation($"Found user {user.Username} with email {user.Email}");

                // Check if the user already has Gitea credentials
                if (!string.IsNullOrEmpty(user.GiteaUsername) && !string.IsNullOrEmpty(user.GiteaToken))
                {
                    _logger.LogInformation($"User already has Gitea credentials: {user.GiteaUsername}");
                    
                    // Verify that the token is still valid
                    bool tokenValid = await VerifyGiteaTokenAsync(user.GiteaUsername, user.GiteaToken);
                    
                    if (tokenValid)
                    {
                        _logger.LogInformation($"Gitea token is valid for user {user.GiteaUsername}");
                        return new GiteaUserResult
                        {
                            Success = true,
                            Username = user.GiteaUsername,
                            AccessToken = user.GiteaToken
                        };
                    }
                    
                    _logger.LogWarning($"Gitea token is invalid for user {user.GiteaUsername}. Creating a new token...");
                    
                    // Generate a new password (we don't know the old one)
                    string giteaPassword = GenerateSecurePassword();
                    
                    // Try to create a new token with admin privileges
                    try
                    {
                        _logger.LogInformation($"Attempting to create a new token for {user.GiteaUsername} using admin credentials");
                        
                        // Assuming we have a method to create a token as admin (not shown here)
                        // For now, we'll reset the user's token to null and create a new Gitea account
                        _logger.LogInformation($"Resetting Gitea credentials for user {user.Username}");
                        user.GiteaUsername = null;
                        user.GiteaToken = null;
                        _userService.UpdateUser(user);
                        
                        // Fall through to create a new Gitea account
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error creating new token for user {user.GiteaUsername}");
                        // Continue with creating a new account
                    }
                }
                
                // Create a new Gitea account
                _logger.LogInformation($"Creating new Gitea account for user {user.Username}");
                
                try
                {
                    // Call EnsureGiteaUserAsync to create/get Gitea account
                    var loginSession = await EnsureGiteaUserAsync(userId.ToString());
                    
                    // Reload user to get updated Gitea credentials
                    user = _userService.GetUserById(userId);
                    
                    if (user == null || string.IsNullOrEmpty(user.GiteaUsername) || string.IsNullOrEmpty(user.GiteaToken))
                    {
                        _logger.LogError($"Failed to create Gitea account for user {userId}");
                        return new GiteaUserResult
                        {
                            Success = false,
                            ErrorMessage = "Failed to create Gitea account"
                        };
                    }
                    
                    _logger.LogInformation($"Successfully created Gitea account: {user.GiteaUsername}");
                    
                    return new GiteaUserResult
                    {
                        Success = true,
                        Username = user.GiteaUsername,
                        AccessToken = user.GiteaToken
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error creating Gitea account for user {user.Username}: {ex.Message}");
                    return new GiteaUserResult
                    {
                        Success = false,
                        ErrorMessage = $"Error creating Gitea account: {ex.Message}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in EnsureGiteaUserWithDetailsAsync for user ID {userId}: {ex.Message}");
                return new GiteaUserResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
        
        /// <summary>
        /// Verifies that a Gitea token is valid
        /// </summary>
        private async Task<bool> VerifyGiteaTokenAsync(string username, string token)
        {
            try
            {
                _logger.LogInformation($"Verifying token for Gitea user {username}");
                
                // Create HTTP client
                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(_giteaService.GetBaseUrl()),
                    Timeout = TimeSpan.FromSeconds(10)
                };
                
                // Create request to get current user info (which requires valid token)
                var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/user");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", token);
                
                // Send request
                var response = await httpClient.SendAsync(request);
                
                bool isValid = response.IsSuccessStatusCode;
                _logger.LogInformation($"Token verification result for {username}: {isValid} (Status code: {response.StatusCode})");
                
                if (!isValid)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Token verification error for {username}: {content}");
                }
                
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verifying token for Gitea user {username}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Link an existing Gitea account with username and password to a DevCommunity user
        /// </summary>
        public async Task<GiteaUserResult> LinkGiteaAccountAsync(int userId, string giteaUsername, string giteaPassword)
        {
            try
            {
                // Get DevCommunity user
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
                
                // Create a token to verify the account exists and credentials are valid
                string tokenName = $"DevCommunity-Link-{DateTime.Now.Ticks}";
                string accessToken = await _giteaService.CreateAccessTokenAsync(
                    giteaUsername,
                    giteaPassword,
                    tokenName);
                
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogError($"Failed to create access token for Gitea user {giteaUsername}");
                    return new GiteaUserResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to authenticate with Gitea. Please check your username and password."
                    };
                }
                
                // Update user with Gitea information
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
                _logger.LogError(ex, $"Error linking Gitea account for user ID {userId}");
                return new GiteaUserResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
        
        /// <summary>
        /// Unlink Gitea account from DevCommunity user
        /// </summary>
        public async Task<bool> UnlinkGiteaAccountAsync(int userId)
        {
            try
            {
                var user = _userService.GetUserById(userId);
                
                if (user == null)
                {
                    _logger.LogError($"User with ID {userId} not found");
                    return false;
                }
                
                // Clear Gitea information
                user.GiteaUsername = null;
                user.GiteaToken = null;
                _userService.UpdateUser(user);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error unlinking Gitea account for user ID {userId}");
                return false;
            }
        }

        /// <summary>
        /// Generate a Gitea username based on DevCommunity username
        /// </summary>
        private string GenerateGiteaUsername(string username)
        {
            // Remove invalid characters for Gitea usernames
            var validUsername = new string(username.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
            
            // Ensure it's at least 3 characters long
            if (validUsername.Length < 3)
            {
                validUsername = validUsername.PadRight(3, 'x');
            }
            
            // Add a unique suffix
            return $"{validUsername}-{DateTime.Now.Ticks % 10000}";
        }

        /// <summary>
        /// Generate a secure random password
        /// </summary>
        private string GenerateSecurePassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
            
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[16]; // 128 bits of randomness
            rng.GetBytes(bytes);
            
            var result = new StringBuilder(16);
            foreach (byte b in bytes)
            {
                result.Append(chars[b % chars.Length]);
            }
            
            return result.ToString();
        }

        /// <summary>
        /// Generate a login URL for automatic login to Gitea
        /// </summary>
        public async Task<string> GetGiteaLoginUrlAsync(int userId)
        {
            try
            {
                var user = _userService.GetUserById(userId);
                
                if (user == null)
                {
                    _logger.LogError($"User with ID {userId} not found");
                    return null;
                }
                
                // Check if user has Gitea account linked
                if (string.IsNullOrEmpty(user.GiteaUsername) || string.IsNullOrEmpty(user.GiteaToken))
                {
                    _logger.LogWarning($"User {userId} does not have a linked Gitea account");
                    return null;
                }
                
                // Generate login session
                return await _giteaService.GenerateLoginSessionAsync(user.GiteaUsername, user.GiteaToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating Gitea login URL for user ID {userId}");
                return null;
            }
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
        Task<string> EnsureGiteaUserAsync(string userId);
        Task<string> EnsureGiteaUserAsync(int userId);
        Task<GiteaUserResult> EnsureGiteaUserWithDetailsAsync(int userId);
        Task<GiteaUserResult> LinkGiteaAccountAsync(int userId, string giteaUsername, string giteaPassword);
        Task<bool> UnlinkGiteaAccountAsync(int userId);
        Task<string> GetGiteaLoginUrlAsync(int userId);
    }
} 