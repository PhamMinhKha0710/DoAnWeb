using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DoAnWeb.GitIntegration
{
    public class TokenResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Sha1 { get; set; }
        [JsonPropertyName("token_last_eight")]
        public string TokenLastEight { get; set; }
        public string[] Scopes { get; set; }
    }

    public class RepositoryResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public bool Private { get; set; }
        public RepositoryOwner Owner { get; set; }
        public string HtmlUrl { get; set; }
        public string Description { get; set; }
        public string DefaultBranch { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int Size { get; set; }
        public int StarsCount { get; set; }
        public int WatchersCount { get; set; }
        public string CloneUrl { get; set; }
        public string SshUrl { get; set; }
    }

    public class RepositoryOwner
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string AvatarUrl { get; set; }
    }

    public class FileContentResponse
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Content { get; set; }
        public long Size { get; set; }
        public string Encoding { get; set; }
        public string Sha { get; set; }
        public string Url { get; set; }
        public string HtmlUrl { get; set; }
    }

    public class SearchRepositoryResponse
    {
        public int Total { get; set; }
        public bool Ok { get; set; }
        public List<RepositoryResponse> Data { get; set; }
    }
} 