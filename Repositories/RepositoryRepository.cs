using DoAnWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace DoAnWeb.Repositories
{
    public class RepositoryRepository : Repository<Repository>, IRepositoryRepository
    {
        public RepositoryRepository(DevCommunityContext context) : base(context)
        {
        }

        public Repository GetRepositoryWithOwner(int repositoryId)
        {
            return _dbSet
                .Include(r => r.Owner)
                .FirstOrDefault(r => r.RepositoryId == repositoryId);
        }

        public IEnumerable<Repository> GetRepositoriesByOwner(int ownerId)
        {
            return _dbSet
                .Include(r => r.Owner)
                .Where(r => r.OwnerId == ownerId)
                .ToList();
        }

        public Repository GetRepositoryWithFiles(int repositoryId)
        {
            return _dbSet
                .Include(r => r.Owner)
                .Include(r => r.RepositoryFiles)
                .FirstOrDefault(r => r.RepositoryId == repositoryId);
        }

        public Repository GetRepositoryWithCommits(int repositoryId)
        {
            return _dbSet
                .Include(r => r.Owner)
                .Include(r => r.RepositoryCommits)
                    .ThenInclude(c => c.Author)
                .FirstOrDefault(r => r.RepositoryId == repositoryId);
        }

        public IEnumerable<Repository> SearchRepositories(string searchTerm)
        {
            return _dbSet
                .Include(r => r.Owner)
                .Where(r => r.RepositoryName.Contains(searchTerm) || r.Description.Contains(searchTerm))
                .ToList();
        }

        public IEnumerable<Repository> GetAllWithOwners()
        {
            return _dbSet
                .Include(r => r.Owner)
                .ToList();
        }
    }
}