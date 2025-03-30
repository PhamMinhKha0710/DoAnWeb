using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DoAnWeb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DoAnWeb.GitIntegration
{
    public class CheckGiteaUsers
    {
        private readonly ILogger<CheckGiteaUsers> _logger;
        private readonly DevCommunityContext _context;
        private readonly string _giteaBaseUrl;
        private readonly string _adminToken;

        public CheckGiteaUsers(
            ILogger<CheckGiteaUsers> logger,
            DevCommunityContext context,
            IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _giteaBaseUrl = configuration["Gitea:BaseUrl"];
            _adminToken = configuration["Gitea:AdminToken"];
        }

        public async Task CheckUsers()
        {
            _logger.LogInformation($"Checking users with Gitea accounts...");

            var users = await _context.Users
                .Where(u => u.GiteaUsername != null && u.GiteaToken != null)
                .ToListAsync();

            _logger.LogInformation($"Found {users.Count} users with Gitea accounts");

            foreach (var user in users)
            {
                _logger.LogInformation($"Checking user: {user.Username} (ID: {user.UserId})");
                _logger.LogInformation($"Gitea username: {user.GiteaUsername}");
                
                bool userExists = await CheckUserExists(user.GiteaUsername);
                bool tokenValid = await CheckTokenValid(user.GiteaToken);
                bool canCreateRepo = tokenValid && await CheckCanCreateRepo(user.GiteaToken);

                _logger.LogInformation($"Status for {user.Username}: User exists: {userExists}, Token valid: {tokenValid}, Can create repo: {canCreateRepo}");

                if (!userExists || !tokenValid)
                {
                    _logger.LogWarning($"User {user.Username} has issues with Gitea account");
                    
                    // Reset Gitea credentials if needed
                    // user.GiteaUsername = null;
                    // user.GiteaToken = null;
                    // _context.SaveChanges();
                }
            }
        }

        private async Task<bool> CheckUserExists(string username)
        {
            try
            {
                using var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(_giteaBaseUrl),
                    Timeout = TimeSpan.FromSeconds(10)
                };

                var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/users/{username}");
                
                var response = await httpClient.SendAsync(request);
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if user {username} exists");
                return false;
            }
        }

        private async Task<bool> CheckTokenValid(string token)
        {
            try
            {
                using var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(_giteaBaseUrl),
                    Timeout = TimeSpan.FromSeconds(10)
                };

                var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/user");
                request.Headers.Authorization = new AuthenticationHeaderValue("token", token);
                
                var response = await httpClient.SendAsync(request);
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking token validity");
                return false;
            }
        }

        private async Task<bool> CheckCanCreateRepo(string token)
        {
            try
            {
                using var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(_giteaBaseUrl),
                    Timeout = TimeSpan.FromSeconds(10)
                };

                var repoName = $"test-repo-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                
                var repoRequest = new
                {
                    name = repoName,
                    description = "Test repository for troubleshooting",
                    @private = false,
                    auto_init = true
                };
                
                var content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(repoRequest),
                    System.Text.Encoding.UTF8,
                    "application/json");
                
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/user/repos");
                request.Headers.Authorization = new AuthenticationHeaderValue("token", token);
                request.Content = content;
                
                var response = await httpClient.SendAsync(request);
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if can create repository");
                return false;
            }
        }
    }
} 