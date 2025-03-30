using System.Collections.Generic;
using System.Threading.Tasks;

namespace DoAnWeb.GitIntegration
{
    public interface IGiteaIntegrationService
    {
        /// <summary>
        /// Tạo token truy cập API cho người dùng Gitea
        /// </summary>
        Task<string> CreateAccessTokenAsync(string username, string password, string tokenName);

        /// <summary>
        /// Tạo người dùng mới trong Gitea
        /// </summary>
        Task<bool> CreateUserAsync(string username, string email, string password, string fullName);

        /// <summary>
        /// Tạo repository mới trong Gitea
        /// </summary>
        Task<RepositoryResponse> CreateRepositoryAsync(string ownerUsername, string accessToken, string repoName, string description, bool isPrivate);

        /// <summary>
        /// Lấy danh sách repository của người dùng
        /// </summary>
        Task<List<RepositoryResponse>> GetUserRepositoriesAsync(string username, string accessToken);

        /// <summary>
        /// Lấy nội dung của một file trong repository
        /// </summary>
        Task<FileContentResponse> GetFileContentAsync(string ownerUsername, string repoName, string filePath, string accessToken, string branch = null);

        /// <summary>
        /// Get contents of a directory from a repository
        /// </summary>
        Task<List<Models.GiteaContent>> GetDirectoryContentsAsync(string ownerUsername, string repoName, string directoryPath, string accessToken, string branch = null);

        /// <summary>
        /// Tìm kiếm repositories trong Gitea
        /// </summary>
        Task<List<RepositoryResponse>> SearchRepositoriesAsync(string keyword, string accessToken);
        
        /// <summary>
        /// Generate a login session for direct login to Gitea without password
        /// </summary>
        Task<string> GenerateLoginSessionAsync(string username, string accessToken);
        
        /// <summary>
        /// Test if the Gitea server is accessible
        /// </summary>
        Task<bool> TestGiteaServerConnectivityAsync();
        
        /// <summary>
        /// Get branches for a repository
        /// </summary>
        Task<List<Models.GiteaBranch>> GetBranchesAsync(string owner, string repo, string accessToken);
        
        /// <summary>
        /// Get commits for a repository
        /// </summary>
        Task<List<Models.GiteaCommit>> GetCommitsAsync(string owner, string repo, string branch, string accessToken, string path = null);
        
        /// <summary>
        /// Create a new branch in a repository
        /// </summary>
        Task<bool> CreateBranchAsync(string owner, string repo, string newBranchName, string sourceBranch, string accessToken);

        /// <summary>
        /// Get the base URL of the Gitea server
        /// </summary>
        string GetBaseUrl();
        
        /// <summary>
        /// Get admin token for accessing Gitea API
        /// </summary>
        string GetAdminToken();
        
        /// <summary>
        /// Get repository details by ID
        /// </summary>
        Task<RepositoryResponse> GetRepositoryByIdAsync(int repositoryId, string accessToken);
    }
} 