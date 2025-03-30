using System;

namespace DoAnWeb.GitIntegration
{
    /// <summary>
    /// Represents the result of a repository creation operation
    /// </summary>
    public class RepositoryResult
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// The ID of the repository in the DevCommunity database
        /// </summary>
        public int RepositoryId { get; set; }
        
        /// <summary>
        /// The ID of the repository in the Gitea system
        /// </summary>
        public int GiteaRepositoryId { get; set; }
        
        /// <summary>
        /// The HTML URL to access the repository in the browser
        /// </summary>
        public string HtmlUrl { get; set; }
        
        /// <summary>
        /// The HTTPS URL for cloning the repository
        /// </summary>
        public string CloneUrl { get; set; }
        
        /// <summary>
        /// The SSH URL for cloning the repository
        /// </summary>
        public string SshUrl { get; set; }
        
        /// <summary>
        /// Error message if the operation failed
        /// </summary>
        public string ErrorMessage { get; set; }
    }
} 