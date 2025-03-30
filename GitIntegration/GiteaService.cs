using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Linq;

namespace DoAnWeb.GitIntegration
{
    /// <summary>
    /// Implementation of IGiteaIntegrationService that connects to a real Gitea instance
    /// </summary>
    public class GiteaService : IGiteaIntegrationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GiteaService> _logger;
        private readonly string _baseUrl;
        private readonly string _adminUsername;
        private readonly string _adminPassword;
        private readonly string _adminToken;

        public GiteaService(
            HttpClient httpClient, 
            IConfiguration configuration,
            ILogger<GiteaService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            
            // Get configuration
            _baseUrl = configuration["Gitea:BaseUrl"] ?? "http://localhost:3000";
            _adminUsername = configuration["Gitea:AdminUsername"];
            _adminPassword = configuration["Gitea:AdminPassword"];
            _adminToken = configuration["Gitea:AdminToken"];
            
            _logger.LogInformation($"Initializing Gitea service with base URL: {_baseUrl}");
            
            // Configure HttpClient
            if (!_httpClient.BaseAddress?.ToString()?.StartsWith(_baseUrl) ?? true)
            {
                _httpClient.BaseAddress = new Uri(_baseUrl);
                _logger.LogInformation($"Set HttpClient BaseAddress to {_baseUrl}");
            }
            
            // Log configuration info
            if (string.IsNullOrEmpty(_adminUsername))
            {
                _logger.LogWarning("Gitea admin username is not configured");
            }
            
            if (string.IsNullOrEmpty(_adminPassword))
            {
                _logger.LogWarning("Gitea admin password is not configured");
            }
            
            if (string.IsNullOrEmpty(_adminToken))
            {
                _logger.LogWarning("Gitea admin token is not configured");
            }
            
            // Set default headers
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "DevCommunity-Application");
            
            // Test the connection asynchronously
            Task.Run(async () => {
                try {
                    var isConnected = await TestGiteaServerConnectivityAsync();
                    if (isConnected) {
                        _logger.LogInformation("Successfully connected to Gitea server");
                    } else {
                        _logger.LogError("Failed to connect to Gitea server. Please check your configuration and ensure the server is running.");
                    }
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error testing connection to Gitea server");
                }
            });
        }

        /// <summary>
        /// Create an access token for a Gitea user
        /// </summary>
        public async Task<string> CreateAccessTokenAsync(string username, string password, string tokenName)
        {
            try
            {
                _logger.LogInformation($"Creating access token for user {username} with token name '{tokenName}'");
                
                // Create Authentication header with base64 encoded username:password
                var authBytes = Encoding.ASCII.GetBytes($"{username}:{password}");
                var authHeader = Convert.ToBase64String(authBytes);
                
                // Create request
                var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/users/{username}/tokens");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
                
                // Create token request body with required scopes
                var tokenRequest = new { 
                    name = tokenName,
                    scopes = new[] { "write:repository", "write:user" } 
                };
                
                var content = new StringContent(
                    JsonSerializer.Serialize(tokenRequest), 
                    Encoding.UTF8, 
                    "application/json");
                request.Content = content;
                
                _logger.LogInformation($"Sending token creation request to Gitea API for user {username}");
                
                // Send request
                var response = await _httpClient.SendAsync(request);
                
                _logger.LogInformation($"Token creation response status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Token creation response: {responseContent}");
                    
                    using var doc = JsonDocument.Parse(responseContent);
                    
                    // Extract token from response
                    if (doc.RootElement.TryGetProperty("sha1", out var tokenElement))
                    {
                        string token = tokenElement.GetString();
                        _logger.LogInformation($"Successfully created token for user {username}");
                        return token;
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to extract token from response: {responseContent}");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Failed to create token for user {username}. Status: {response.StatusCode}, Response: {errorContent}");
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating token for user {username}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create a new user in Gitea
        /// </summary>
        public async Task<bool> CreateUserAsync(string username, string email, string password, string fullName)
        {
            try
            {
                // Validate input parameters
                if (string.IsNullOrWhiteSpace(username))
                {
                    _logger.LogError("Cannot create user: Username is empty");
                    return false;
                }
                
                if (string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogError("Cannot create user: Email is empty");
                    return false;
                }
                
                if (string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogError("Cannot create user: Password is empty");
                    return false;
                }
                
                // Validate password strength - Gitea typically requires at least 6 characters
                if (password.Length < 6)
                {
                    _logger.LogError("Cannot create user: Password must be at least 6 characters long");
                    return false;
                }
                
                // First check if Gitea server is accessible
                var isServerAccessible = await TestGiteaServerConnectivityAsync();
                if (!isServerAccessible)
                {
                    _logger.LogError($"Gitea server at {_baseUrl} is not accessible. Cannot create user {username}");
                    return false;
                }
                
                // Use admin token for creating users
                var adminToken = _adminToken;
                
                _logger.LogInformation($"Creating user {username} with email {email}");
                _logger.LogInformation($"Admin credentials: Username={_adminUsername}, Token={(adminToken != null ? "Present" : "Missing")}");
                
                // If admin token is not provided, get one
                if (string.IsNullOrEmpty(adminToken))
                {
                    _logger.LogInformation($"No admin token provided, attempting to create one with username: {_adminUsername}");
                    adminToken = await CreateAccessTokenAsync(_adminUsername, _adminPassword, "Admin API Token");
                    
                    if (string.IsNullOrEmpty(adminToken))
                    {
                        _logger.LogError($"Failed to create admin token for {_adminUsername}. Check admin credentials in appsettings.json");
                        return false;
                    }
                    
                    _logger.LogInformation($"Successfully created admin token for {_adminUsername}");
                }
                
                // Attempt to create the user
                return await AttemptUserCreation(username, email, password, fullName, adminToken) || 
                       await AttemptUserCreationWithFallbackEmail(username, email, password, fullName, adminToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"HTTP request error creating user {username}: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating user {username}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Attempt to create a user with the given credentials
        /// </summary>
        private async Task<bool> AttemptUserCreation(string username, string email, string password, string fullName, string adminToken)
        {
            try
            {
                // Create request
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/users");
                request.Headers.Authorization = new AuthenticationHeaderValue("token", adminToken);
                
                // Create user request body
                var userRequest = new
                {
                    username = username,
                    email = email,
                    password = password,
                    full_name = fullName ?? username,
                    must_change_password = false,
                    send_notify = false
                };
                
                var jsonContent = JsonSerializer.Serialize(userRequest);
                _logger.LogInformation($"Request body: {jsonContent}");
                
                var content = new StringContent(
                    jsonContent, 
                    Encoding.UTF8, 
                    "application/json");
                request.Content = content;
                
                _logger.LogInformation($"Sending request to create user to: {_baseUrl}/api/v1/admin/users");
                
                // Send request
                var response = await _httpClient.SendAsync(request);
                
                _logger.LogInformation($"Received response with status code: {(int)response.StatusCode} {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"User {username} created successfully. Response: {responseContent}");
                    return true;
                }
                
                // Check if user already exists (409 Conflict)
                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"User {username} already exists. Response: {responseContent}");
                    return true;
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to create user {username}. Status: {response.StatusCode}, Response: {errorContent}");
                
                // Check for common errors
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    _logger.LogError($"Authentication error. Admin token may be invalid or expired. Status: {response.StatusCode}, Response: {errorContent}");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogError($"API endpoint not found. Check if Gitea server is configured correctly: {_baseUrl}/api/v1/admin/users, Response: {errorContent}");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    _logger.LogError($"Bad request when creating user. This could be due to invalid data format or missing required fields. Response: {errorContent}");
                }
                else 
                {
                    _logger.LogError($"Unexpected error when creating user. Status code: {response.StatusCode}, Response: {errorContent}");
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in AttemptUserCreation for user {username}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Attempt to create a user with a fallback email if the original email fails
        /// This is useful if the original email is rejected by Gitea validation rules
        /// </summary>
        private async Task<bool> AttemptUserCreationWithFallbackEmail(string username, string email, string password, string fullName, string adminToken)
        {
            try
            {
                // Generate a fallback email that follows a predictable pattern
                string fallbackEmail = $"{username.ToLower()}@gitea.devcommunity.local";
                
                _logger.LogInformation($"Attempting user creation with fallback email: {fallbackEmail}");
                
                // Create request
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/users");
                request.Headers.Authorization = new AuthenticationHeaderValue("token", adminToken);
                
                // Create user request body with fallback email
                var userRequest = new
                {
                    username = username,
                    email = fallbackEmail,  // Use fallback email here
                    password = password,
                    full_name = fullName ?? username,
                    must_change_password = false,
                    send_notify = false
                };
                
                var jsonContent = JsonSerializer.Serialize(userRequest);
                _logger.LogInformation($"Request body (fallback): {jsonContent}");
                
                var content = new StringContent(
                    jsonContent, 
                    Encoding.UTF8, 
                    "application/json");
                request.Content = content;
                
                // Send request
                var response = await _httpClient.SendAsync(request);
                
                _logger.LogInformation($"Received fallback response with status code: {(int)response.StatusCode} {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"User {username} created successfully with fallback email. Response: {responseContent}");
                    return true;
                }
                
                // Check if user already exists (409 Conflict)
                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"User {username} already exists (fallback attempt). Response: {responseContent}");
                    return true;
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to create user {username} with fallback email. Status: {response.StatusCode}, Response: {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in AttemptUserCreationWithFallbackEmail for user {username}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test if the Gitea server is accessible
        /// </summary>
        public async Task<bool> TestGiteaServerConnectivityAsync()
        {
            try
            {
                _logger.LogInformation($"Testing connectivity to Gitea server at {_baseUrl}");
                
                // Create a simple GET request to the Gitea API version endpoint
                var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/version");
                
                // Set a short timeout
                var timeoutCancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;
                
                // Send request
                var response = await _httpClient.SendAsync(request, timeoutCancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Gitea server is accessible. Version information: {content}");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"Gitea server returned non-success status code: {response.StatusCode}");
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogError($"Timeout connecting to Gitea server at {_baseUrl}");
                return false;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"HTTP request error connecting to Gitea server at {_baseUrl}: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error testing Gitea server connectivity: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create a new repository in Gitea
        /// </summary>
        public async Task<RepositoryResponse> CreateRepositoryAsync(string ownerUsername, string accessToken, string repoName, string description, bool isPrivate)
        {
            try
            {
                // Create request
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/user/repos");
                request.Headers.Authorization = new AuthenticationHeaderValue("token", accessToken);
                
                // Create repository request body
                var repoRequest = new
                {
                    name = repoName,
                    description = description,
                    @private = isPrivate,
                    auto_init = true,  // Initialize with README.md
                    default_branch = "main",
                    gitignores = "VisualStudio",
                    license = "MIT"
                };
                
                var content = new StringContent(
                    JsonSerializer.Serialize(repoRequest), 
                    Encoding.UTF8, 
                    "application/json");
                request.Content = content;
                
                // Send request
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    // Parse response to RepositoryResponse
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var repository = JsonSerializer.Deserialize<RepositoryResponse>(responseContent, options);
                    
                    // Set additional properties
                    repository.HtmlUrl = $"{_baseUrl}/{ownerUsername}/{repoName}";
                    repository.CloneUrl = $"{_baseUrl}/{ownerUsername}/{repoName}.git";
                    repository.SshUrl = $"git@{new Uri(_baseUrl).Host}:{ownerUsername}/{repoName}.git";
                    
                    _logger.LogInformation($"Repository {repoName} created successfully for user {ownerUsername}");
                    return repository;
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to create repository {repoName}. Status: {response.StatusCode}, Response: {errorContent}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating repository {repoName} for user {ownerUsername}");
                return null;
            }
        }

        /// <summary>
        /// Get repositories for a user from Gitea
        /// </summary>
        public async Task<List<RepositoryResponse>> GetUserRepositoriesAsync(string username, string accessToken)
        {
            try
            {
                // Create request
                var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/user/repos");
                request.Headers.Authorization = new AuthenticationHeaderValue("token", accessToken);
                
                // Send request
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    // Parse response to List<RepositoryResponse>
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var repositories = JsonSerializer.Deserialize<List<RepositoryResponse>>(responseContent, options);
                    
                    // Set additional properties for each repository
                    foreach (var repo in repositories)
                    {
                        repo.HtmlUrl = $"{_baseUrl}/{username}/{repo.Name}";
                        repo.CloneUrl = $"{_baseUrl}/{username}/{repo.Name}.git";
                        repo.SshUrl = $"git@{new Uri(_baseUrl).Host}:{username}/{repo.Name}.git";
                    }
                    
                    return repositories;
                }
                
                _logger.LogWarning($"Failed to get repositories for user {username}. Status: {response.StatusCode}");
                return new List<RepositoryResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting repositories for user {username}");
                return new List<RepositoryResponse>();
            }
        }

        /// <summary>
        /// Get content of a file from a repository
        /// </summary>
        public async Task<FileContentResponse> GetFileContentAsync(string ownerUsername, string repoName, string filePath, string accessToken, string branch = null)
        {
            try
            {
                // Create request URL
                string requestUrl = $"/api/v1/repos/{ownerUsername}/{repoName}/contents/{filePath}";
                
                // Add branch reference if specified
                if (!string.IsNullOrEmpty(branch))
                {
                    requestUrl += $"?ref={branch}";
                }
                
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("token", accessToken);
                
                // Send request
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    // Parse response to FileContentResponse
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var fileContent = JsonSerializer.Deserialize<FileContentResponse>(responseContent, options);
                    return fileContent;
                }
                
                _logger.LogWarning($"Failed to get file content from {ownerUsername}/{repoName}/{filePath}. Status: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting file content from {ownerUsername}/{repoName}/{filePath}");
                return null;
            }
        }
        
        /// <summary>
        /// Get contents of a directory from a repository
        /// </summary>
        public async Task<List<Models.GiteaContent>> GetDirectoryContentsAsync(string ownerUsername, string repoName, string directoryPath, string accessToken, string branch = null)
        {
            try
            {
                _logger.LogInformation($"Getting directory contents for {ownerUsername}/{repoName}/{directoryPath}, branch={branch ?? "default"}");
                
                // Create request URL
                string requestUrl = $"/api/v1/repos/{ownerUsername}/{repoName}/contents/";
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    requestUrl += directoryPath;
                }
                
                if (!string.IsNullOrEmpty(branch))
                {
                    requestUrl += $"?ref={branch}";
                }
                
                _logger.LogDebug($"Request URL: {requestUrl}");
                
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("token", accessToken);
                
                // Log token prefix for debugging (don't log the full token)
                if (!string.IsNullOrEmpty(accessToken) && accessToken.Length > 8)
                {
                    _logger.LogDebug($"Using token starting with: {accessToken.Substring(0, 8)}...");
                }
                else
                {
                    _logger.LogWarning("Access token is missing or too short");
                }
                
                // Send request
                _logger.LogDebug("Sending request to Gitea API...");
                var response = await _httpClient.SendAsync(request);
                
                _logger.LogDebug($"Response status code: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug($"Response content length: {responseContent?.Length ?? 0} chars");
                    
                    if (string.IsNullOrEmpty(responseContent))
                    {
                        _logger.LogWarning("Response content is empty");
                        return new List<Models.GiteaContent>();
                    }
                    
                    try
                    {
                    // Parse response to List<GiteaContent>
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var contents = JsonSerializer.Deserialize<List<Models.GiteaContent>>(responseContent, options);
                        _logger.LogInformation($"Successfully deserialized {contents?.Count ?? 0} items from directory");
                        
                        // Log some info about the contents
                        if (contents != null && contents.Any())
                        {
                            foreach (var item in contents.Take(5))
                            {
                                _logger.LogDebug($"Item: {item.Name}, Type: {item.Type}");
                            }
                            
                            if (contents.Count > 5)
                            {
                                _logger.LogDebug($"... and {contents.Count - 5} more items");
                            }
                        }
                        
                        return contents ?? new List<Models.GiteaContent>();
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, $"JSON deserialization error: {ex.Message}");
                        _logger.LogDebug($"Response content snapshot: {responseContent?.Substring(0, Math.Min(500, responseContent.Length))}...");
                return new List<Models.GiteaContent>();
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Failed to get directory contents from {ownerUsername}/{repoName}/{directoryPath}. Status: {response.StatusCode}, Response: {errorContent}");
                    return new List<Models.GiteaContent>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting directory contents from {ownerUsername}/{repoName}/{directoryPath}");
                return new List<Models.GiteaContent>();
            }
        }

        /// <summary>
        /// Search repositories in Gitea
        /// </summary>
        public async Task<List<RepositoryResponse>> SearchRepositoriesAsync(string keyword, string accessToken)
        {
            try
            {
                // Create request
                var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/repos/search?q={Uri.EscapeDataString(keyword)}&limit=50");
                
                // Add authorization if provided
                if (!string.IsNullOrEmpty(accessToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("token", accessToken);
                }
                
                // Send request
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    // Parse response
                    using var doc = JsonDocument.Parse(responseContent);
                    if (doc.RootElement.TryGetProperty("data", out var dataElement))
                    {
                        // Parse repositories
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        
                        var repositories = JsonSerializer.Deserialize<List<RepositoryResponse>>(dataElement.GetRawText(), options);
                        
                        // Set additional properties for each repository
                        foreach (var repo in repositories)
                        {
                            var owner = repo.Owner?.Login ?? "unknown";
                            repo.HtmlUrl = $"{_baseUrl}/{owner}/{repo.Name}";
                            repo.CloneUrl = $"{_baseUrl}/{owner}/{repo.Name}.git";
                            repo.SshUrl = $"git@{new Uri(_baseUrl).Host}:{owner}/{repo.Name}.git";
                        }
                        
                        return repositories;
                    }
                }
                
                _logger.LogWarning($"Failed to search repositories with keyword '{keyword}'. Status: {response.StatusCode}");
                return new List<RepositoryResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching repositories with keyword '{keyword}'");
                return new List<RepositoryResponse>();
            }
        }

        /// <summary>
        /// Generate a login session for direct login to Gitea without password
        /// </summary>
        public async Task<string> GenerateLoginSessionAsync(string username, string accessToken)
        {
            try
            {
                _logger.LogInformation($"Generating login session for Gitea user: {username}");
                
                // First try with the API endpoint for creating sessions (if available in Gitea version)
                try
                {
                    // Create request to the Gitea API
                    var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/sessions");
                    request.Headers.Authorization = new AuthenticationHeaderValue("token", accessToken);
                    
                    // Create session request body
                    var sessionRequest = new
                    {
                        username = username,
                        remember = true
                    };
                    
                    var content = new StringContent(
                        JsonSerializer.Serialize(sessionRequest), 
                        Encoding.UTF8, 
                        "application/json");
                    request.Content = content;
                    
                    // Send request
                    var response = await _httpClient.SendAsync(request);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        _logger.LogInformation($"Session API response: {responseContent}");
                        
                        using var doc = JsonDocument.Parse(responseContent);
                        
                        // Extract session ID from response
                        if (doc.RootElement.TryGetProperty("id", out var idElement))
                        {
                            var sessionId = idElement.GetString();
                            _logger.LogInformation($"Generated session ID: {sessionId}");
                            return sessionId;
                        }
                        
                        // If id property doesn't exist but response was successful, try to get cookie
                        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
                        {
                            foreach (var cookie in cookies)
                            {
                                _logger.LogInformation($"Received cookie: {cookie}");
                                if (cookie.StartsWith("i_like_gitea="))
                                {
                                    // Extract cookie value
                                    var cookieValue = cookie.Split(';')[0].Replace("i_like_gitea=", "");
                                    _logger.LogInformation($"Extracted session cookie: {cookieValue}");
                                    return cookieValue;
                                }
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Session API failed with status: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error using sessions API, falling back to token-based login");
                }
                
                // Fallback method: Construct a direct login URL using the token
                // This works with most Gitea versions
                var loginUrl = $"{_baseUrl}/user/login?remember=on&user_name={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(accessToken)}";
                _logger.LogInformation($"Generated direct login URL using token");
                
                return loginUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating login session for user {username}");
                return null;
            }
        }

        /// <summary>
        /// Get branches for a repository
        /// </summary>
        public async Task<List<Models.GiteaBranch>> GetBranchesAsync(string owner, string repo, string accessToken)
        {
            try
            {
                // Create request
                var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/repos/{owner}/{repo}/branches");
                request.Headers.Authorization = new AuthenticationHeaderValue("token", accessToken);
                
                // Send request
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    // Parse response to List<GiteaBranch>
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var branches = JsonSerializer.Deserialize<List<Models.GiteaBranch>>(responseContent, options);
                    return branches ?? new List<Models.GiteaBranch>();
                }
                
                _logger.LogWarning($"Failed to get branches for repository {owner}/{repo}. Status: {response.StatusCode}");
                return new List<Models.GiteaBranch>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting branches for repository {owner}/{repo}");
                return new List<Models.GiteaBranch>();
            }
        }
        
        /// <summary>
        /// Get commits for a repository
        /// </summary>
        public async Task<List<Models.GiteaCommit>> GetCommitsAsync(string owner, string repo, string branch, string accessToken, string path = null)
        {
            try
            {
                // Create request URL
                string requestUrl = $"/api/v1/repos/{owner}/{repo}/commits?sha={branch}";
                if (!string.IsNullOrEmpty(path))
                {
                    requestUrl += $"&path={path}";
                }
                
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("token", accessToken);
                
                // Send request
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    // Parse response to List<GiteaCommit>
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var commits = JsonSerializer.Deserialize<List<Models.GiteaCommit>>(responseContent, options);
                    return commits ?? new List<Models.GiteaCommit>();
                }
                
                _logger.LogWarning($"Failed to get commits for repository {owner}/{repo}. Status: {response.StatusCode}");
                return new List<Models.GiteaCommit>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting commits for repository {owner}/{repo}");
                return new List<Models.GiteaCommit>();
            }
        }
        
        /// <summary>
        /// Create a new branch in a repository
        /// </summary>
        public async Task<bool> CreateBranchAsync(string owner, string repo, string newBranchName, string sourceBranch, string accessToken)
        {
            try
            {
                // First, get the commit SHA of the source branch
                var branchesRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/repos/{owner}/{repo}/branches/{sourceBranch}");
                branchesRequest.Headers.Authorization = new AuthenticationHeaderValue("token", accessToken);
                
                var branchResponse = await _httpClient.SendAsync(branchesRequest);
                
                if (!branchResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to get source branch {sourceBranch} for repository {owner}/{repo}. Status: {branchResponse.StatusCode}");
                    return false;
                }
                
                var branchContent = await branchResponse.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var sourceBranchInfo = JsonSerializer.Deserialize<Models.GiteaBranch>(branchContent, options);
                if (sourceBranchInfo?.Commit?.Id == null)
                {
                    _logger.LogWarning($"Source branch {sourceBranch} does not have a commit ID");
                    return false;
                }
                
                // Create request to create new branch
                var createRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/repos/{owner}/{repo}/branches");
                createRequest.Headers.Authorization = new AuthenticationHeaderValue("token", accessToken);
                
                // Create branch request body
                var branchRequest = new
                {
                    new_branch_name = newBranchName,
                    old_branch_name = sourceBranch
                };
                
                var content = new StringContent(
                    JsonSerializer.Serialize(branchRequest), 
                    Encoding.UTF8, 
                    "application/json");
                createRequest.Content = content;
                
                // Send request
                var response = await _httpClient.SendAsync(createRequest);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Branch {newBranchName} created successfully in repository {owner}/{repo}");
                    return true;
                }
                
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to create branch {newBranchName} in repository {owner}/{repo}. Status: {response.StatusCode}, Response: {responseContent}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating branch {newBranchName} in repository {owner}/{repo}");
                return false;
            }
        }

        /// <summary>
        /// Get the base URL of the Gitea server
        /// </summary>
        public string GetBaseUrl()
        {
            return _baseUrl;
        }

        /// <summary>
        /// Get admin token for accessing Gitea API
        /// </summary>
        public string GetAdminToken()
        {
            if (string.IsNullOrEmpty(_adminToken))
            {
                _logger.LogWarning("Admin token is not configured in the application settings");
            }
            return _adminToken;
        }

        /// <summary>
        /// Get repository details by ID
        /// </summary>
        public async Task<RepositoryResponse> GetRepositoryByIdAsync(int repositoryId, string accessToken)
        {
            try
            {
                _logger.LogInformation($"Getting repository with ID {repositoryId}");
                
                // Create request
                var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/repositories/{repositoryId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("token", accessToken);
                
                // Send request
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    // Parse response to RepositoryResponse
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var repository = JsonSerializer.Deserialize<RepositoryResponse>(responseContent, options);
                    
                    if (repository != null && repository.Owner != null)
                    {
                        // Set additional properties
                        repository.HtmlUrl = $"{_baseUrl}/{repository.Owner.Login}/{repository.Name}";
                        repository.CloneUrl = $"{_baseUrl}/{repository.Owner.Login}/{repository.Name}.git";
                        repository.SshUrl = $"git@{new Uri(_baseUrl).Host}:{repository.Owner.Login}/{repository.Name}.git";
                    }
                    
                    _logger.LogInformation($"Repository with ID {repositoryId} retrieved successfully");
                    return repository;
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to get repository with ID {repositoryId}. Status: {response.StatusCode}, Response: {errorContent}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting repository with ID {repositoryId}");
                return null;
            }
        }
    }
} 