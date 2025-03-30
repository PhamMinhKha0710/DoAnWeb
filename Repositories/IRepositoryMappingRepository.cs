using DoAnWeb.Models;
using System.Collections.Generic;

namespace DoAnWeb.Repositories
{
    public interface IRepositoryMappingRepository : IRepository<RepositoryMapping>
    {
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
    }
} 