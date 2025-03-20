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

        public Task<FileContentResponse> GetFileContentAsync(string ownerUsername, string repoName, string filePath, string accessToken)
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

        public Task<List<RepositoryResponse>> SearchRepositoriesAsync(string keyword, string accessToken)
        {
            // Trả về danh sách rỗng
            return Task.FromResult(new List<RepositoryResponse>());
        }
    }
} 
