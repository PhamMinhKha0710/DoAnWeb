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
        Task<FileContentResponse> GetFileContentAsync(string ownerUsername, string repoName, string filePath, string accessToken);

        /// <summary>
        /// Tìm kiếm repositories trong Gitea
        /// </summary>
        Task<List<RepositoryResponse>> SearchRepositoriesAsync(string keyword, string accessToken);
    }
} 