using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DoAnWeb.Models;
using DoAnWeb.Services;
using DoAnWeb.GitIntegration.Models;
using System.Net.Http;
using System.Linq;

namespace DoAnWeb.GitIntegration
{
    public class GiteaRepositoryService : IGiteaRepositoryService
    {
        private readonly IGiteaIntegrationService _giteaService;
        private readonly IGiteaUserSyncService _userSyncService;
        private readonly IRepositoryService _repositoryService;
        private readonly IRepositoryMappingService _repositoryMappingService;
        private readonly ILogger<GiteaRepositoryService> _logger;

        public GiteaRepositoryService(
            IGiteaIntegrationService giteaService,
            IGiteaUserSyncService userSyncService,
            IRepositoryService repositoryService,
            IRepositoryMappingService repositoryMappingService,
            ILogger<GiteaRepositoryService> logger)
        {
            _giteaService = giteaService;
            _userSyncService = userSyncService;
            _repositoryService = repositoryService;
            _repositoryMappingService = repositoryMappingService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new repository both in DevCommunity and Gitea
        /// </summary>
        public async Task<RepositoryResult> CreateRepositoryAsync(int userId, string name, string description, bool isPrivate)
        {
            try
            {
                _logger.LogInformation($"Starting repository creation for user ID {userId}, name: {name}");
                
                // Ensure user has Gitea account
                _logger.LogInformation($"Ensuring Gitea user for ID {userId}");
                var userResult = await _userSyncService.EnsureGiteaUserWithDetailsAsync(userId);
                
                if (!userResult.Success)
                {
                    _logger.LogError($"Couldn't ensure Gitea user: {userResult.ErrorMessage}");
                    return new RepositoryResult
                    {
                        Success = false,
                        ErrorMessage = $"Failed to create repository: {userResult.ErrorMessage}"
                    };
                }
                
                _logger.LogInformation($"Successfully ensured Gitea user. Username: {userResult.Username}, Has Token: {!string.IsNullOrEmpty(userResult.AccessToken)}");

                // Create repository in Gitea
                _logger.LogInformation($"Creating repository in Gitea with name: {name}, owner: {userResult.Username}");
                var giteaRepo = await _giteaService.CreateRepositoryAsync(
                    userResult.Username,
                    userResult.AccessToken,
                    name,
                    description,
                    isPrivate);
                
                if (giteaRepo == null)
                {
                    _logger.LogError($"Failed to create repository {name} in Gitea for user {userResult.Username}");
                    
                    // Try to retrieve user and test token validity
                    try 
                    {
                        var httpClient = new HttpClient
                        {
                            BaseAddress = new Uri(_giteaService.GetBaseUrl())
                        };
                        
                        // Test token validity
                        var tokenRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/user");
                        tokenRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", userResult.AccessToken);
                        
                        var tokenResponse = await httpClient.SendAsync(tokenRequest);
                        _logger.LogError($"Token validation check: {tokenResponse.StatusCode}");
                        
                        if (!tokenResponse.IsSuccessStatusCode)
                        {
                            var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                            _logger.LogError($"Token validation error: {errorContent}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while validating user token");
                    }
                    
                    return new RepositoryResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to create repository in Gitea"
                    };
                }
                
                _logger.LogInformation($"Repository created successfully in Gitea with ID: {giteaRepo.Id}");

                // Create repository in DevCommunity
                var repository = new Repository
                {
                    RepositoryName = name,
                    Description = description,
                    OwnerId = userId,
                    Visibility = isPrivate ? "Private" : "Public",
                    DefaultBranch = "main",
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };
                
                _logger.LogInformation($"Creating repository in DevCommunity database");
                _repositoryService.CreateRepository(repository);
                
                _logger.LogInformation($"Created repository in DevCommunity with ID {repository.RepositoryId} and in Gitea with ID {giteaRepo.Id}");
                
                // Create mapping between DevCommunity and Gitea repositories
                _logger.LogInformation($"Creating mapping between DevCommunity repo {repository.RepositoryId} and Gitea repo {giteaRepo.Id}");
                _repositoryMappingService.CreateMapping(
                    repository.RepositoryId, 
                    giteaRepo.Id, 
                    giteaRepo.HtmlUrl, 
                    giteaRepo.CloneUrl, 
                    giteaRepo.SshUrl);
                
                _logger.LogInformation($"Repository creation completed successfully");
                return new RepositoryResult
                {
                    Success = true,
                    RepositoryId = repository.RepositoryId,
                    GiteaRepositoryId = giteaRepo.Id,
                    HtmlUrl = giteaRepo.HtmlUrl,
                    CloneUrl = giteaRepo.CloneUrl,
                    SshUrl = giteaRepo.SshUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating repository {name} for user {userId}: {ex.Message}");
                return new RepositoryResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Get repositories for a user from Gitea
        /// </summary>
        public async Task<List<RepositoryResponse>> GetUserRepositoriesAsync(int userId)
        {
            try
            {
                // Ensure user has Gitea account
                var userResult = await _userSyncService.EnsureGiteaUserWithDetailsAsync(userId);
                
                if (!userResult.Success)
                {
                    _logger.LogError($"Couldn't ensure Gitea user: {userResult.ErrorMessage}");
                    return new List<RepositoryResponse>();
                }
                
                // Get repositories from Gitea
                return await _giteaService.GetUserRepositoriesAsync(userResult.Username, userResult.AccessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting repositories for user ID {userId}");
                return new List<RepositoryResponse>();
            }
        }

        /// <summary>
        /// Get content of a file from a repository
        /// </summary>
        public async Task<FileContentResponse> GetFileContentAsync(int userId, string ownerUsername, string repoName, string filePath, string branch = null)
        {
            try
            {
                // Ensure user has Gitea account
                var userResult = await _userSyncService.EnsureGiteaUserWithDetailsAsync(userId);
                
                if (!userResult.Success)
                {
                    _logger.LogError($"Couldn't ensure Gitea user: {userResult.ErrorMessage}");
                    return null;
                }
                
                // Get file content from Gitea
                return await _giteaService.GetFileContentAsync(ownerUsername, repoName, filePath, userResult.AccessToken, branch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting file content from {ownerUsername}/{repoName}/{filePath}");
                return null;
            }
        }

        /// <summary>
        /// Search for repositories in Gitea
        /// </summary>
        public async Task<List<RepositoryResponse>> SearchRepositoriesAsync(string keyword, int? userId = null)
        {
            try
            {
                string accessToken = null;
                
                // If userId is provided, ensure user has Gitea account
                if (userId.HasValue)
                {
                    var userResult = await _userSyncService.EnsureGiteaUserWithDetailsAsync(userId.Value);
                    
                    if (userResult.Success)
                    {
                        accessToken = userResult.AccessToken;
                    }
                    else
                    {
                        _logger.LogWarning($"Couldn't ensure Gitea user for ID {userId.Value}: {userResult.ErrorMessage}. Proceeding with anonymous search.");
                    }
                }
                
                // Search repositories in Gitea
                return await _giteaService.SearchRepositoriesAsync(keyword, accessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching repositories with keyword {keyword}");
                return new List<RepositoryResponse>();
            }
        }
        
        /// <summary>
        /// Get branches for a repository
        /// </summary>
        public async Task<List<GitIntegration.Models.GiteaBranch>> GetBranchesAsync(int userId, string owner, string repo)
        {
            try
            {
                // Ensure user has Gitea account
                var userResult = await _userSyncService.EnsureGiteaUserWithDetailsAsync(userId);
                
                if (!userResult.Success)
                {
                    _logger.LogError($"Couldn't ensure Gitea user: {userResult.ErrorMessage}");
                    return new List<GitIntegration.Models.GiteaBranch>();
                }
                
                // Get branches from Gitea
                return await _giteaService.GetBranchesAsync(owner, repo, userResult.AccessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting branches for repository {owner}/{repo}");
                return new List<GitIntegration.Models.GiteaBranch>();
            }
        }
        
        /// <summary>
        /// Get contents of a directory from a repository
        /// </summary>
        public async Task<GitContentResponse> GetDirectoryContentAsync(int userId, string owner, string repo, string path, string branch = null)
        {
            try
            {
                // Ensure user has Gitea account
                var userResult = await _userSyncService.EnsureGiteaUserWithDetailsAsync(userId);
                
                if (!userResult.Success)
                {
                    _logger.LogError($"Couldn't ensure Gitea user: {userResult.ErrorMessage}");
                    return new GitContentResponse();
                }
                
                // Get directory contents from Gitea
                var contents = await _giteaService.GetDirectoryContentsAsync(owner, repo, path, userResult.AccessToken, branch);
                
                if (contents == null || !contents.Any())
                {
                    return new GitContentResponse 
                    { 
                        Type = "directory", 
                        Path = path,
                        Files = new List<GitIntegration.Models.GiteaContent>() 
                    };
                }
                
                // Create response
                var response = new GitContentResponse
                {
                    Type = "directory",
                    Name = string.IsNullOrEmpty(path) ? repo : System.IO.Path.GetFileName(path),
                    Path = path,
                    Files = contents
                };
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting directory contents for {owner}/{repo}/{path}");
                return new GitContentResponse();
            }
        }
        
        /// <summary>
        /// Get commit history for a repository
        /// </summary>
        public async Task<List<GitIntegration.Models.GiteaCommit>> GetCommitHistoryAsync(int userId, string owner, string repo, string branch, string path = null)
        {
            try
            {
                // Ensure user has Gitea account
                var userResult = await _userSyncService.EnsureGiteaUserWithDetailsAsync(userId);
                
                if (!userResult.Success)
                {
                    _logger.LogError($"Couldn't ensure Gitea user: {userResult.ErrorMessage}");
                    return new List<GitIntegration.Models.GiteaCommit>();
                }
                
                // Get commits from Gitea
                return await _giteaService.GetCommitsAsync(owner, repo, branch, userResult.AccessToken, path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting commit history for repository {owner}/{repo}");
                return new List<GitIntegration.Models.GiteaCommit>();
            }
        }
        
        /// <summary>
        /// Create a new branch in a repository
        /// </summary>
        public async Task<bool> CreateBranchAsync(int userId, string owner, string repo, string newBranchName, string sourceBranch)
        {
            try
            {
                // Ensure user has Gitea account
                var userResult = await _userSyncService.EnsureGiteaUserWithDetailsAsync(userId);
                
                if (!userResult.Success)
                {
                    _logger.LogError($"Couldn't ensure Gitea user: {userResult.ErrorMessage}");
                    return false;
                }
                
                // Create branch in Gitea
                return await _giteaService.CreateBranchAsync(owner, repo, newBranchName, sourceBranch, userResult.AccessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating branch in repository {owner}/{repo}");
                return false;
            }
        }

        /// <summary>
        /// Get branches for a repository by Gitea repository ID
        /// </summary>
        public async Task<List<GitIntegration.Models.GiteaBranch>> GetRepositoryBranchesAsync(int giteaRepositoryId)
        {
            try
            {
                _logger.LogInformation($"Getting branches for Gitea repository ID {giteaRepositoryId}");
                
                // Sử dụng admin token để truy cập repository
                string adminToken = _giteaService.GetAdminToken();
                if (string.IsNullOrEmpty(adminToken))
                {
                    _logger.LogError("Failed to get admin token for Gitea access");
                    return new List<GitIntegration.Models.GiteaBranch>();
                }
                
                // Lấy chi tiết repository từ Gitea dựa trên ID
                var repositoryDetails = await _giteaService.GetRepositoryByIdAsync(giteaRepositoryId, adminToken);
                if (repositoryDetails == null)
                {
                    _logger.LogError($"Could not find repository with ID {giteaRepositoryId} in Gitea");
                    return new List<GitIntegration.Models.GiteaBranch>();
                }
                
                // Lấy danh sách nhánh từ Gitea
                _logger.LogInformation($"Found repository {repositoryDetails.Owner.Login}/{repositoryDetails.Name}, getting branches");
                return await _giteaService.GetBranchesAsync(
                    repositoryDetails.Owner.Login, 
                    repositoryDetails.Name, 
                    adminToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting branches for Gitea repository ID {giteaRepositoryId}");
                return new List<GitIntegration.Models.GiteaBranch>();
            }
        }
        
        /// <summary>
        /// Get contents of a directory from a repository by Gitea repository ID
        /// </summary>
        public async Task<List<GitIntegration.Models.GiteaContent>> GetDirectoryContentsAsync(int giteaRepositoryId, string branch, string path = null)
        {
            try
            {
                _logger.LogInformation($"Getting directory contents for Gitea repository ID {giteaRepositoryId}, branch {branch}, path {path ?? "root"}");
                
                // Sử dụng admin token để truy cập repository
                string adminToken = _giteaService.GetAdminToken();
                if (string.IsNullOrEmpty(adminToken))
                {
                    _logger.LogError("Failed to get admin token for Gitea access");
                    return new List<GitIntegration.Models.GiteaContent>();
                }
                
                // Lấy chi tiết repository từ Gitea dựa trên ID
                var repositoryDetails = await _giteaService.GetRepositoryByIdAsync(giteaRepositoryId, adminToken);
                if (repositoryDetails == null)
                {
                    _logger.LogError($"Could not find repository with ID {giteaRepositoryId} in Gitea");
                    return new List<GitIntegration.Models.GiteaContent>();
                }
                
                _logger.LogInformation($"Found repository {repositoryDetails.Owner.Login}/{repositoryDetails.Name}, getting directory contents");
                
                // Lấy nội dung thư mục từ Gitea
                return await _giteaService.GetDirectoryContentsAsync(
                    repositoryDetails.Owner.Login, 
                    repositoryDetails.Name, 
                    path ?? "", 
                    adminToken, 
                    branch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting directory contents for Gitea repository ID {giteaRepositoryId}");
                return new List<GitIntegration.Models.GiteaContent>();
            }
        }
        
        /// <summary>
        /// Get file content from a repository by Gitea repository ID
        /// </summary>
        public async Task<FileContentResponse> GetRepositoryFileContentAsync(int giteaRepositoryId, string branch, string path)
        {
            try
            {
                _logger.LogInformation($"Getting file content for Gitea repository ID {giteaRepositoryId}, branch {branch}, path {path}");
                
                // Sử dụng admin token để truy cập repository
                string adminToken = _giteaService.GetAdminToken();
                if (string.IsNullOrEmpty(adminToken))
                {
                    _logger.LogError("Failed to get admin token for Gitea access");
                    return null;
                }
                
                // Lấy chi tiết repository từ Gitea dựa trên ID
                var repositoryDetails = await _giteaService.GetRepositoryByIdAsync(giteaRepositoryId, adminToken);
                if (repositoryDetails == null)
                {
                    _logger.LogError($"Could not find repository with ID {giteaRepositoryId} in Gitea");
                    return null;
                }
                
                _logger.LogInformation($"Found repository {repositoryDetails.Owner.Login}/{repositoryDetails.Name}, getting file content");
                
                // Lấy nội dung file từ Gitea
                return await _giteaService.GetFileContentAsync(
                    repositoryDetails.Owner.Login, 
                    repositoryDetails.Name, 
                    path, 
                    adminToken,
                    branch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting file content for Gitea repository ID {giteaRepositoryId}, path {path}");
                return null;
            }
        }
    }
} 