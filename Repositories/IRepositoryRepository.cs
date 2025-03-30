using DoAnWeb.Models;

namespace DoAnWeb.Repositories
{
    public interface IRepositoryRepository : IRepository<Repository>
    {
        // Get repository with owner information
        Repository GetRepositoryWithOwner(int repositoryId);
        
        // Get repositories by owner
        IEnumerable<Repository> GetRepositoriesByOwner(int ownerId);
        
        // Get repository with files
        Repository GetRepositoryWithFiles(int repositoryId);
        
        // Get repository with commits
        Repository GetRepositoryWithCommits(int repositoryId);
        
        // Search repositories by name
        IEnumerable<Repository> SearchRepositories(string searchTerm);
        
        // Get all repositories with owner information loaded
        IEnumerable<Repository> GetAllWithOwners();
    }
}