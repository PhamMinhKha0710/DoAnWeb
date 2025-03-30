using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DoAnWeb.GitIntegration.Models
{
    /// <summary>
    /// Represents a branch in a Gitea repository
    /// </summary>
    public class GiteaBranch
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("commit")]
        public GiteaCommitInfo Commit { get; set; }
        
        [JsonPropertyName("protected")]
        public bool Protected { get; set; }
    }
    
    /// <summary>
    /// Represents commit information in a Gitea repository
    /// </summary>
    public class GiteaCommitInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("message")]
        public string Message { get; set; }
        
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
    
    /// <summary>
    /// Represents a commit in a Gitea repository
    /// </summary>
    public class GiteaCommit
    {
        [JsonPropertyName("sha")]
        public string Sha { get; set; }
        
        [JsonPropertyName("commit")]
        public GiteaCommitDetails Commit { get; set; }
        
        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; }
        
        [JsonPropertyName("author")]
        public GiteaUser Author { get; set; }
        
        [JsonPropertyName("committer")]
        public GiteaUser Committer { get; set; }
    }
    
    /// <summary>
    /// Represents commit details in a Gitea repository
    /// </summary>
    public class GiteaCommitDetails
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }
        
        [JsonPropertyName("url")]
        public string Url { get; set; }
        
        [JsonPropertyName("author")]
        public GiteaCommitAuthor Author { get; set; }
        
        [JsonPropertyName("committer")]
        public GiteaCommitAuthor Committer { get; set; }
    }
    
    /// <summary>
    /// Represents a commit author in a Gitea repository
    /// </summary>
    public class GiteaCommitAuthor
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("email")]
        public string Email { get; set; }
        
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }
    }
    
    /// <summary>
    /// Represents a user in Gitea
    /// </summary>
    public class GiteaUser
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("login")]
        public string Login { get; set; }
        
        [JsonPropertyName("full_name")]
        public string FullName { get; set; }
        
        [JsonPropertyName("email")]
        public string Email { get; set; }
        
        [JsonPropertyName("avatar_url")]
        public string AvatarUrl { get; set; }
    }
    
    /// <summary>
    /// Represents content in a Gitea repository
    /// </summary>
    public class GiteaContent
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("path")]
        public string Path { get; set; }
        
        [JsonPropertyName("sha")]
        public string Sha { get; set; }
        
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        [JsonPropertyName("size")]
        public long Size { get; set; }
        
        [JsonPropertyName("url")]
        public string Url { get; set; }
        
        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; }
        
        [JsonPropertyName("download_url")]
        public string DownloadUrl { get; set; }
        
        [JsonPropertyName("content")]
        public string Content { get; set; }
        
        [JsonPropertyName("encoding")]
        public string Encoding { get; set; }
        
        // Additional properties for UI display
        [JsonIgnore]
        public string CommitMessage { get; set; }
        
        [JsonIgnore]
        public DateTime? CommitDate { get; set; }
    }
} 