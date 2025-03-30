using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Security.Claims;
using System;
using System.Text.Json;
using DoAnWeb.GitIntegration;
using DoAnWeb.Services;
using Microsoft.Extensions.Logging;

namespace DoAnWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class DiagnosticsController : ControllerBase
    {
        private readonly IGiteaIntegrationService _giteaService;
        private readonly IGiteaUserSyncService _giteaUserSyncService;
        private readonly IUserService _userService;
        private readonly ILogger<DiagnosticsController> _logger;

        public DiagnosticsController(
            IGiteaIntegrationService giteaService,
            IGiteaUserSyncService giteaUserSyncService,
            IUserService userService,
            ILogger<DiagnosticsController> logger)
        {
            _giteaService = giteaService;
            _giteaUserSyncService = giteaUserSyncService;
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("gitea/test")]
        public async Task<IActionResult> TestGiteaConnection()
        {
            try
            {
                var result = await _giteaService.TestGiteaServerConnectivityAsync();
                return Ok(new { Success = result, Message = result ? "Gitea server is accessible" : "Gitea server is not accessible" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing Gitea connectivity");
                return StatusCode(500, new { Success = false, Message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet("gitea/user/{userId}")]
        public async Task<IActionResult> TestGiteaUser(int userId)
        {
            try
            {
                var response = new
                {
                    UserId = userId,
                    UserInfo = (object)null,
                    GiteaUserExists = false,
                    TokenValid = false,
                    RepositoryCreationTest = (object)null,
                    Errors = (object)null
                };

                // Get user from database
                var user = _userService.GetUserById(userId);
                if (user == null)
                {
                    return NotFound(new { Success = false, Message = $"User with ID {userId} not found" });
                }

                var userInfo = new
                {
                    Username = user.Username,
                    Email = user.Email,
                    GiteaUsername = user.GiteaUsername,
                    HasGiteaToken = !string.IsNullOrEmpty(user.GiteaToken)
                };

                // Check if the user has Gitea account linked
                if (string.IsNullOrEmpty(user.GiteaUsername) || string.IsNullOrEmpty(user.GiteaToken))
                {
                    return Ok(new
                    {
                        Success = false,
                        Message = "User has no Gitea account linked",
                        UserInfo = userInfo
                    });
                }

                // Test if the Gitea user exists
                var userRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/users/{user.GiteaUsername}");
                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(_giteaService.GetBaseUrl())
                };
                
                var userResponse = await httpClient.SendAsync(userRequest);
                var giteaUserExists = userResponse.IsSuccessStatusCode;
                
                // Test token validity
                var tokenRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/user");
                tokenRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", user.GiteaToken);
                
                var tokenResponse = await httpClient.SendAsync(tokenRequest);
                var tokenValid = tokenResponse.IsSuccessStatusCode;
                
                // Try creating a repository
                var repoCreationResults = new { Success = false, Message = "", StatusCode = 0 };
                
                if (tokenValid)
                {
                    try
                    {
                        var repoName = $"test-repo-{DateTime.Now.Ticks}";
                        
                        var repoRequest = new
                        {
                            name = repoName,
                            description = "Test repository for troubleshooting",
                            @private = false,
                            auto_init = true,
                            default_branch = "main",
                            gitignores = "VisualStudio",
                            license = "MIT"
                        };
                        
                        var repoContent = new StringContent(
                            JsonSerializer.Serialize(repoRequest),
                            System.Text.Encoding.UTF8,
                            "application/json");
                        
                        var repoRequestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/v1/user/repos");
                        repoRequestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", user.GiteaToken);
                        repoRequestMessage.Content = repoContent;
                        
                        var repoResponse = await httpClient.SendAsync(repoRequestMessage);
                        var repoContent2 = await repoResponse.Content.ReadAsStringAsync();
                        
                        repoCreationResults = new
                        {
                            Success = repoResponse.IsSuccessStatusCode,
                            Message = repoContent2,
                            StatusCode = (int)repoResponse.StatusCode
                        };
                    }
                    catch (Exception ex)
                    {
                        repoCreationResults = new
                        {
                            Success = false,
                            Message = $"Error: {ex.Message}",
                            StatusCode = 500
                        };
                    }
                }
                
                return Ok(new
                {
                    Success = true,
                    UserInfo = userInfo,
                    GiteaUserExists = giteaUserExists,
                    TokenValid = tokenValid,
                    RepositoryCreationTest = repoCreationResults
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error testing Gitea user {userId}");
                return StatusCode(500, new { Success = false, Message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost("gitea/reset-user/{userId}")]
        public async Task<IActionResult> ResetGiteaUser(int userId)
        {
            try
            {
                // Get user from database
                var user = _userService.GetUserById(userId);
                if (user == null)
                {
                    return NotFound(new { Success = false, Message = $"User with ID {userId} not found" });
                }

                // Force unlink the old account
                user.GiteaUsername = null;
                user.GiteaToken = null;
                _userService.UpdateUser(user);

                // Create a new Gitea account and link it
                var userResult = await _giteaUserSyncService.EnsureGiteaUserWithDetailsAsync(userId);
                
                return Ok(new
                {
                    Success = userResult.Success,
                    Message = userResult.Success ? "Gitea account reset successfully" : userResult.ErrorMessage,
                    NewUsername = userResult.Username
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resetting Gitea user {userId}");
                return StatusCode(500, new { Success = false, Message = $"Error: {ex.Message}" });
            }
        }
    }
} 