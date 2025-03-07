using DoAnWeb.Models;

namespace DoAnWeb.Services
{
    public interface IRepositoryService
    {
        // Get all repositories
        IEnumerable<Repository> GetAllRepositories();
        
        // Get repository by id
        Repository GetRepositoryById(int id);
        
        // Get repository with owner
        Repository GetRepositoryWithOwner(int id);
        
        // Get repositories by owner
        IEnumerable<Repository> GetRepositoriesByOwner(int ownerId);
        
        // Get repository with files
        Repository GetRepositoryWithFiles(int id);
        
        // Get repository with commits
        Repository GetRepositoryWithCommits(int id);
        
        // Search repositories
        IEnumerable<Repository> SearchRepositories(string searchTerm);
        
        // Create repository
        void CreateRepository(Repository repository);
        
        // Update repository
        void UpdateRepository(Repository repository);
        
        // Delete repository
        void DeleteRepository(int id);
        
        // Add file to repository
        void AddFile(RepositoryFile file);
        
        // Update file in repository
        void UpdateFile(RepositoryFile file);
        
        // Delete file from repository
        void DeleteFile(int fileId);
        
        // Create commit
        void CreateCommit(RepositoryCommit commit);
    }
}