using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DoAnWeb.Models;
using DoAnWeb.Services;

namespace DoAnWeb.GitIntegration
{
    public class GiteaRepositoryService : IGiteaRepositoryService
    {
        private readonly IGiteaIntegrationService _giteaService;
        private readonly IGiteaUserSyncService _userSyncService;
        private readonly IRepositoryService _repositoryService;
        private readonly ILogger<GiteaRepositoryService> _logger;

        public GiteaRepositoryService(
            IGiteaIntegrationService giteaService,
            IGiteaUserSyncService userSyncService,
            IRepositoryService repositoryService,
            ILogger<GiteaRepositoryService> logger)
        {
            _giteaService = giteaService;
            _userSyncService = userSyncService;
            _repositoryService = repositoryService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo repository mới với cả trong DevCommunity và Gitea
        /// </summary>
        public async Task<RepositoryResult> CreateRepositoryAsync(int userId, string name, string description, bool isPrivate)
        {
            try
            {
                // Đảm bảo người dùng có trong Gitea
                var userResult = await _userSyncService.EnsureGiteaUserAsync(userId);
                
                if (!userResult.Success)
                {
                    return new RepositoryResult
                    {
                        Success = false,
                        ErrorMessage = $"Couldn't ensure Gitea user: {userResult.ErrorMessage}"
                    };
                }
                
                // Tạo repository trong Gitea
                var giteaRepo = await _giteaService.CreateRepositoryAsync(
                    userResult.Username,
                    userResult.AccessToken,
                    name,
                    description,
                    isPrivate);
                
                if (giteaRepo == null)
                {
                    return new RepositoryResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to create Gitea repository"
                    };
                }
                
                // Tạo repository trong DevCommunity
                var devCommunityRepo = new Repository
                {
                    OwnerId = userId,
                    RepositoryName = name,
                    Description = description,
                    Visibility = isPrivate ? "Private" : "Public",
                    DefaultBranch = giteaRepo.DefaultBranch,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };
                
                _repositoryService.CreateRepository(devCommunityRepo);
                
                // Trả về thông tin kết hợp
                return new RepositoryResult
                {
                    Success = true,
                    RepositoryId = devCommunityRepo.RepositoryId,
                    GiteaRepositoryId = giteaRepo.Id,
                    HtmlUrl = giteaRepo.HtmlUrl,
                    CloneUrl = giteaRepo.CloneUrl,
                    SshUrl = giteaRepo.SshUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating repository for user ID {userId}");
                return new RepositoryResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Lấy danh sách repository từ Gitea cho người dùng
        /// </summary>
        public async Task<List<RepositoryResponse>> GetUserRepositoriesAsync(int userId)
        {
            try
            {
                // Đảm bảo người dùng có trong Gitea
                var userResult = await _userSyncService.EnsureGiteaUserAsync(userId);
                
                if (!userResult.Success)
                {
                    _logger.LogError($"Couldn't ensure Gitea user: {userResult.ErrorMessage}");
                    return new List<RepositoryResponse>();
                }
                
                // Lấy danh sách repository từ Gitea
                return await _giteaService.GetUserRepositoriesAsync(
                    userResult.Username,
                    userResult.AccessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting repositories for user ID {userId}");
                return new List<RepositoryResponse>();
            }
        }

        /// <summary>
        /// Lấy nội dung file từ Gitea
        /// </summary>
        public async Task<FileContentResponse> GetFileContentAsync(int userId, string ownerUsername, string repoName, string filePath)
        {
            try
            {
                // Đảm bảo người dùng có trong Gitea
                var userResult = await _userSyncService.EnsureGiteaUserAsync(userId);
                
                if (!userResult.Success)
                {
                    _logger.LogError($"Couldn't ensure Gitea user: {userResult.ErrorMessage}");
                    return null;
                }
                
                // Lấy nội dung file từ Gitea
                return await _giteaService.GetFileContentAsync(
                    ownerUsername,
                    repoName,
                    filePath,
                    userResult.AccessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting file content for user ID {userId}");
                return null;
            }
        }

        /// <summary>
        /// Tìm kiếm repositories trong Gitea
        /// </summary>
        public async Task<List<RepositoryResponse>> SearchRepositoriesAsync(string keyword, int? userId = null)
        {
            try
            {
                string accessToken = null;
                
                // Nếu có ID người dùng, lấy token của họ
                if (userId.HasValue)
                {
                    var userResult = await _userSyncService.EnsureGiteaUserAsync(userId.Value);
                    
                    if (userResult.Success)
                    {
                        accessToken = userResult.AccessToken;
                    }
                }
                
                // Tìm kiếm repositories
                return await _giteaService.SearchRepositoriesAsync(keyword, accessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching repositories");
                return new List<RepositoryResponse>();
            }
        }
    }

    public class RepositoryResult
    {
        public bool Success { get; set; }
        public int RepositoryId { get; set; }
        public int GiteaRepositoryId { get; set; }
        public string HtmlUrl { get; set; }
        public string CloneUrl { get; set; }
        public string SshUrl { get; set; }
        public string ErrorMessage { get; set; }
    }

    public interface IGiteaRepositoryService
    {
        Task<RepositoryResult> CreateRepositoryAsync(int userId, string name, string description, bool isPrivate);
        Task<List<RepositoryResponse>> GetUserRepositoriesAsync(int userId);
        Task<FileContentResponse> GetFileContentAsync(int userId, string ownerUsername, string repoName, string filePath);
        Task<List<RepositoryResponse>> SearchRepositoriesAsync(string keyword, int? userId = null);
    }
} 