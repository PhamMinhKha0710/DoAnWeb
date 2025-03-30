using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace DoAnWeb.GitIntegration
{
    // Triển khai đơn giản cho IGiteaIntegrationService
    public class SimpleGiteaService : IGiteaIntegrationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public SimpleGiteaService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public Task<string> CreateAccessTokenAsync(string username, string password, string tokenName)
        {
            // Trả về một token giả
            return Task.FromResult("dummy_token_123");
        }

        public Task<bool> CreateUserAsync(string username, string email, string password, string fullName)
        {
            // Giả định thành công
            return Task.FromResult(true);
        }

        public Task<RepositoryResponse> CreateRepositoryAsync(string ownerUsername, string accessToken, string repoName, string description, bool isPrivate)
        {
            // Trả về repository giả
            var repo = new RepositoryResponse
            {
                Id = 1,
                Name = repoName,
                Description = description,
                Private = isPrivate,
                Owner = new RepositoryOwner
                {
                    Id = 1,
                    Login = ownerUsername,
                    FullName = ownerUsername
                }
            };
            
            return Task.FromResult(repo);
        }

        public Task<List<RepositoryResponse>> GetUserRepositoriesAsync(string username, string accessToken)
        {
            // Trả về danh sách rỗng
            return Task.FromResult(new List<RepositoryResponse>());
        }

        /// <summary>
        /// Get content of a file from a repository with branch specification
        /// </summary>
        public Task<FileContentResponse> GetFileContentAsync(string ownerUsername, string repoName, string filePath, string accessToken, string branch = null)
        {
            // Trả về nội dung file giả
            var fileContent = new FileContentResponse
            {
                Name = "example.txt",
                Path = filePath,
                Content = "Example content",
                Encoding = "utf-8"
            };
            
            return Task.FromResult(fileContent);
        }

        public Task<List<Models.GiteaContent>> GetDirectoryContentsAsync(string ownerUsername, string repoName, string directoryPath, string accessToken, string branch = null)
        {
            // Trả về danh sách tệp giả
            var contents = new List<GitIntegration.Models.GiteaContent>
            {
                new GitIntegration.Models.GiteaContent 
                { 
                    Name = "example.txt", 
                    Path = "example.txt",
                    Type = "file",
                    Size = 100,
                    HtmlUrl = $"{_configuration["Gitea:BaseUrl"]}/{ownerUsername}/{repoName}/src/branch/main/example.txt"
                },
                new GitIntegration.Models.GiteaContent
                {
                    Name = "docs",
                    Path = "docs",
                    Type = "dir",
                    HtmlUrl = $"{_configuration["Gitea:BaseUrl"]}/{ownerUsername}/{repoName}/src/branch/main/docs"
                }
            };
            
            return Task.FromResult(contents);
        }

        public Task<List<RepositoryResponse>> SearchRepositoriesAsync(string keyword, string accessToken)
        {
            // Trả về danh sách rỗng
            return Task.FromResult(new List<RepositoryResponse>());
        }
        
        /// <summary>
        /// Generate a login session for direct login to Gitea without password
        /// </summary>
        public Task<string> GenerateLoginSessionAsync(string username, string accessToken)
        {
            // Return a dummy session ID in the simple implementation
            return Task.FromResult("dummy_session_" + Guid.NewGuid().ToString("N"));
        }
        
        /// <summary>
        /// Test if the Gitea server is accessible
        /// </summary>
        public Task<bool> TestGiteaServerConnectivityAsync()
        {
            // Simple implementation always returns true since this is a mock service
            return Task.FromResult(true);
        }
        
        /// <summary>
        /// Get branches for a repository
        /// </summary>
        public Task<List<GitIntegration.Models.GiteaBranch>> GetBranchesAsync(string owner, string repo, string accessToken)
        {
            // Return a dummy list of branches
            var branches = new List<GitIntegration.Models.GiteaBranch>
            {
                new GitIntegration.Models.GiteaBranch
                {
                    Name = "main",
                    Protected = true,
                    Commit = new GitIntegration.Models.GiteaCommitInfo
                    {
                        Id = "dummy_commit_id",
                        Message = "Initial commit",
                        Url = $"https://example.com/{owner}/{repo}/commit/dummy_commit_id"
                    }
                }
            };
            
            return Task.FromResult(branches);
        }
        
        /// <summary>
        /// Get commits for a repository
        /// </summary>
        public Task<List<GitIntegration.Models.GiteaCommit>> GetCommitsAsync(string owner, string repo, string branch, string accessToken, string path = null)
        {
            // Return a dummy list of commits
            var commits = new List<GitIntegration.Models.GiteaCommit>
            {
                new GitIntegration.Models.GiteaCommit
                {
                    Sha = "dummy_commit_sha",
                    HtmlUrl = $"https://example.com/{owner}/{repo}/commit/dummy_commit_sha",
                    Commit = new GitIntegration.Models.GiteaCommitDetails
                    {
                        Message = "Initial commit",
                        Url = $"https://example.com/{owner}/{repo}/commit/dummy_commit_sha",
                        Author = new GitIntegration.Models.GiteaCommitAuthor
                        {
                            Name = "Dummy Author",
                            Email = "dummy@example.com",
                            Date = DateTime.Now.AddDays(-1)
                        },
                        Committer = new GitIntegration.Models.GiteaCommitAuthor
                        {
                            Name = "Dummy Committer",
                            Email = "dummy@example.com",
                            Date = DateTime.Now.AddDays(-1)
                        }
                    },
                    Author = new GitIntegration.Models.GiteaUser
                    {
                        Id = 1,
                        Login = "dummy_user",
                        FullName = "Dummy User",
                        Email = "dummy@example.com",
                        AvatarUrl = "https://example.com/avatars/dummy_user"
                    },
                    Committer = new GitIntegration.Models.GiteaUser
                    {
                        Id = 1,
                        Login = "dummy_user",
                        FullName = "Dummy User",
                        Email = "dummy@example.com",
                        AvatarUrl = "https://example.com/avatars/dummy_user"
                    }
                }
            };
            
            return Task.FromResult(commits);
        }
        
        /// <summary>
        /// Create a new branch in a repository
        /// </summary>
        public Task<bool> CreateBranchAsync(string owner, string repo, string newBranchName, string sourceBranch, string accessToken)
        {
            // Simply return success for the mock implementation
            return Task.FromResult(true);
        }

        /// <summary>
        /// Get the base URL of the Gitea server
        /// </summary>
        public string GetBaseUrl()
        {
            return "http://localhost:3000";
        }

        /// <summary>
        /// Get admin token for accessing Gitea API
        /// </summary>
        public string GetAdminToken()
        {
            return "dummy_admin_token_123";
        }

        /// <summary>
        /// Get repository details by ID
        /// </summary>
        public Task<RepositoryResponse> GetRepositoryByIdAsync(int repositoryId, string accessToken)
        {
            // Return a dummy repository response
            var repo = new RepositoryResponse
            {
                Id = repositoryId,
                Name = "dummy-repo",
                FullName = "dummy-user/dummy-repo",
                Private = false,
                DefaultBranch = "main",
                Owner = new RepositoryOwner
                {
                    Id = 1,
                    Login = "dummy-user",
                    FullName = "Dummy User",
                    Email = "dummy@example.com",
                    AvatarUrl = "https://example.com/avatars/dummy_user"
                },
                HtmlUrl = $"{GetBaseUrl()}/dummy-user/dummy-repo",
                CloneUrl = $"{GetBaseUrl()}/dummy-user/dummy-repo.git",
                SshUrl = $"git@{new Uri(GetBaseUrl()).Host}:dummy-user/dummy-repo.git"
            };
            
            return Task.FromResult(repo);
        }
    }
} 
