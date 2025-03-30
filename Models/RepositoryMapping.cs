using System;

namespace DoAnWeb.Models
{
    /// <summary>
    /// Maps DevCommunity repositories to Gitea repositories
    /// </summary>
    public class RepositoryMapping
    {
        /// <summary>
        /// Primary key for the mapping
        /// </summary>
        public int MappingId { get; set; }
        
        /// <summary>
        /// ID of the repository in DevCommunity
        /// </summary>
        public int DevCommunityRepositoryId { get; set; }
        
        /// <summary>
        /// ID of the repository in Gitea
        /// </summary>
        public int GiteaRepositoryId { get; set; }
        
        /// <summary>
        /// The HTML URL to access the repository in Gitea
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
        /// When the mapping was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// When the mapping was last synchronized
        /// </summary>
        public DateTime? LastSyncDate { get; set; }
        
        /// <summary>
        /// Navigation property to the DevCommunity repository
        /// </summary>
        public virtual Repository Repository { get; set; }
    }
} 