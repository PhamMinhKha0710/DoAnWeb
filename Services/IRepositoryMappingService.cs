using DoAnWeb.Models;
using System.Collections.Generic;

namespace DoAnWeb.Services
{
    public interface IRepositoryMappingService
    {
        /// <summary>
        /// Create a new mapping between DevCommunity and Gitea repositories
        /// </summary>
        void CreateMapping(int devCommunityRepositoryId, int giteaRepositoryId, string htmlUrl, string cloneUrl, string sshUrl);
        
        /// <summary>
        /// Get mapping by DevCommunity repository ID
        /// </summary>
        RepositoryMapping GetByDevCommunityId(int repositoryId);
        
        /// <summary>
        /// Get mapping by Gitea repository ID
        /// </summary>
        RepositoryMapping GetByGiteaId(int giteaRepositoryId);
        
        /// <summary>
        /// Get all mappings for a user's repositories
        /// </summary>
        IEnumerable<RepositoryMapping> GetMappingsByUserId(int userId);
        
        /// <summary>
        /// Update an existing mapping
        /// </summary>
        void UpdateMapping(RepositoryMapping mapping);
        
        /// <summary>
        /// Delete a mapping
        /// </summary>
        void DeleteMapping(int mappingId);
    }
} 