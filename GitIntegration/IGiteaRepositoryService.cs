using System.Collections.Generic;
using System.Threading.Tasks;
using DoAnWeb.Models;
using DoAnWeb.GitIntegration.Models;

namespace DoAnWeb.GitIntegration
{
    public interface IGiteaRepositoryService
    {
        /// <summary>
        /// Create a new repository both in DevCommunity and Gitea
        /// </summary>
        Task<RepositoryResult> CreateRepositoryAsync(int userId, string name, string description, bool isPrivate);
        
        /// <summary>
        /// Get repositories for a user from Gitea
        /// </summary>
        Task<List<RepositoryResponse>> GetUserRepositoriesAsync(int userId);
        
        /// <summary>
        /// Search for repositories in Gitea
        /// </summary>
        Task<List<RepositoryResponse>> SearchRepositoriesAsync(string searchTerm, int? userId = null);
        
        /// <summary>
        /// Get content of a file from a repository
        /// </summary>
        Task<FileContentResponse> GetFileContentAsync(int userId, string owner, string repo, string path, string branch = null);
        
        /// <summary>
        /// Get contents of a directory from a repository
        /// </summary>
        Task<GitContentResponse> GetDirectoryContentAsync(int userId, string owner, string repo, string path, string branch = null);
        
        /// <summary>
        /// Get branches for a repository
        /// </summary>
        Task<List<GiteaBranch>> GetBranchesAsync(int userId, string owner, string repo);
        
        /// <summary>
        /// Get commit history for a repository
        /// </summary>
        Task<List<GiteaCommit>> GetCommitHistoryAsync(int userId, string owner, string repo, string branch, string path = null);
        
        /// <summary>
        /// Create a new branch in a repository
        /// </summary>
        Task<bool> CreateBranchAsync(int userId, string owner, string repo, string newBranchName, string sourceBranch);
        
        /// <summary>
        /// Get branches for a repository by Gitea repository ID
        /// </summary>
        Task<List<GiteaBranch>> GetRepositoryBranchesAsync(int giteaRepositoryId);
        
        /// <summary>
        /// Get contents of a directory from a repository by Gitea repository ID
        /// </summary>
        Task<List<GiteaContent>> GetDirectoryContentsAsync(int giteaRepositoryId, string branch, string path = null);
        
        /// <summary>
        /// Get file content from a repository by Gitea repository ID
        /// </summary>
        Task<FileContentResponse> GetRepositoryFileContentAsync(int giteaRepositoryId, string branch, string path);
    }
} 