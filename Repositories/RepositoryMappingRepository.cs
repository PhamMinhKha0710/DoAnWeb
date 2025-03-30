using DoAnWeb.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace DoAnWeb.Repositories
{
    public class RepositoryMappingRepository : Repository<RepositoryMapping>, IRepositoryMappingRepository
    {
        public RepositoryMappingRepository(DevCommunityContext context) : base(context)
        {
        }

        public RepositoryMapping GetByDevCommunityId(int repositoryId)
        {
            return _dbSet
                .Include(m => m.Repository)
                .FirstOrDefault(m => m.DevCommunityRepositoryId == repositoryId);
        }

        public RepositoryMapping GetByGiteaId(int giteaRepositoryId)
        {
            return _dbSet
                .Include(m => m.Repository)
                .FirstOrDefault(m => m.GiteaRepositoryId == giteaRepositoryId);
        }

        public IEnumerable<RepositoryMapping> GetMappingsByUserId(int userId)
        {
            return _dbSet
                .Include(m => m.Repository)
                .Where(m => m.Repository.OwnerId == userId)
                .ToList();
        }
    }
} 