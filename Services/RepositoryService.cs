using DoAnWeb.Models;
using DoAnWeb.Repositories;
using Microsoft.Extensions.Logging;

namespace DoAnWeb.Services
{
    public class RepositoryService : IRepositoryService
    {
        private readonly IRepositoryRepository _repositoryRepository;
        private readonly IRepository<RepositoryFile> _fileRepository;
        private readonly IRepository<RepositoryCommit> _commitRepository;
        private readonly ILogger<RepositoryService> _logger;

        public RepositoryService(
            IRepositoryRepository repositoryRepository,
            IRepository<RepositoryFile> fileRepository,
            IRepository<RepositoryCommit> commitRepository,
            ILogger<RepositoryService> logger)
        {
            _repositoryRepository = repositoryRepository;
            _fileRepository = fileRepository;
            _commitRepository = commitRepository;
            _logger = logger;
        }

        public IEnumerable<Repository> GetAllRepositories()
        {
            return _repositoryRepository.GetAll();
        }

        public Repository GetRepositoryById(int id)
        {
            return _repositoryRepository.GetById(id);
        }

        public Repository GetRepositoryWithOwner(int id)
        {
            return _repositoryRepository.GetRepositoryWithOwner(id);
        }

        public IEnumerable<Repository> GetRepositoriesByOwner(int ownerId)
        {
            return _repositoryRepository.GetRepositoriesByOwner(ownerId);
        }

        public Repository GetRepositoryWithFiles(int id)
        {
            try
            {
                return _repositoryRepository.GetRepositoryWithFiles(id);
            }
            catch (Exception ex) when (ex.Message.Contains("Invalid column name 'FileSize'") || 
                                      ex.Message.Contains("Invalid column name 'UpdatedDate'"))
            {
                // Handle the case where the database schema doesn't match the model
                // Get the repository without files first
                var repository = _repositoryRepository.GetById(id);
                if (repository == null) return null;
                
                try
                {
                    // Try to get files directly from the repository context
                    var files = _fileRepository.GetAll().Where(f => f.RepositoryId == id).ToList();
                    
                    // Set default values for missing columns
                    foreach (var file in files)
                    {
                        file.UpdatedDate = repository.UpdatedDate ?? DateTime.Now;
                        file.FileSize = file.FileContent != null ? file.FileContent.Length : 0;
                    }
                    
                    repository.RepositoryFiles = files;
                }
                catch (Exception)
                {
                    // If we still have issues, just return an empty collection
                    repository.RepositoryFiles = new List<RepositoryFile>();
                }
                
                return repository;
            }
        }

        public Repository GetRepositoryWithCommits(int id)
        {
            try
            {
                return _repositoryRepository.GetRepositoryWithCommits(id);
            }
            catch (Exception ex) when (ex.Message.Contains("Invalid column name 'CommitHash'"))
            {
                // Handle the case where the database schema doesn't match the model
                // Get the repository without commits first
                var repository = _repositoryRepository.GetById(id);
                if (repository == null) return null;
                
                try
                {
                    // Try to get commits directly from the repository context
                    var commits = _commitRepository.GetAll().Where(c => c.RepositoryId == id).ToList();
                    
                    // Set default values for missing columns
                    foreach (var commit in commits)
                    {
                        // Generate a simple hash based on commit ID if CommitHash is missing
                        commit.CommitHash = $"temp-{commit.CommitId}";
                    }
                    
                    repository.RepositoryCommits = commits;
                }
                catch (Exception)
                {
                    // If we still have issues, just return an empty collection
                    repository.RepositoryCommits = new List<RepositoryCommit>();
                }
                
                return repository;
            }
        }

        public IEnumerable<Repository> SearchRepositories(string searchTerm)
        {
            return _repositoryRepository.SearchRepositories(searchTerm);
        }

        public void CreateRepository(Repository repository)
        {
            // Set creation date
            repository.CreatedDate = DateTime.Now;
            repository.UpdatedDate = DateTime.Now;
            repository.DefaultBranch = "main";

            // Create repository
            _repositoryRepository.Add(repository);
            _repositoryRepository.Save();
        }

        public void UpdateRepository(Repository repository)
        {
            // Get existing repository
            var existingRepository = _repositoryRepository.GetById(repository.RepositoryId);
            if (existingRepository == null)
                throw new ArgumentException("Repository not found");

            // Update repository properties
            existingRepository.RepositoryName = repository.RepositoryName;
            existingRepository.Description = repository.Description;
            existingRepository.Visibility = repository.Visibility;
            existingRepository.UpdatedDate = DateTime.Now;

            // Update repository
            _repositoryRepository.Update(existingRepository);
            _repositoryRepository.Save();
        }

        public void DeleteRepository(int id)
        {
            _repositoryRepository.Delete(id);
            _repositoryRepository.Save();
        }

        public void AddFile(RepositoryFile file)
        {
            // Check if repository exists
            if (file.RepositoryId == null)
                throw new ArgumentException("Repository ID cannot be null");
                
            var repository = _repositoryRepository.GetById(file.RepositoryId.Value);
            if (repository == null)
                throw new ArgumentException("Repository not found");

            // Set file properties that are missing in the database but used in views
            file.UpdatedDate = DateTime.Now;
            file.FileSize = file.FileContent != null ? file.FileContent.Length : 0;

            // Add file
            _fileRepository.Add(file);
            _fileRepository.Save();
        }

        public void UpdateFile(RepositoryFile file)
        {
            // Get existing file
            var existingFile = _fileRepository.GetById(file.FileId);
            if (existingFile == null)
                throw new ArgumentException("File not found");

            // Update file properties
            existingFile.FilePath = file.FilePath;
            existingFile.FileContent = file.FileContent;
            existingFile.FileHash = file.FileHash;
            
            // Update properties that are missing in the database but used in views
            existingFile.UpdatedDate = DateTime.Now;
            existingFile.FileSize = file.FileContent != null ? file.FileContent.Length : 0;

            // Update file
            _fileRepository.Update(existingFile);
            _fileRepository.Save();
        }

        public void DeleteFile(int fileId)
        {
            _fileRepository.Delete(fileId);
            _fileRepository.Save();
        }

        public void CreateCommit(RepositoryCommit commit)
        {
            // Check if repository exists
            if (commit.RepositoryId == null)
                throw new ArgumentException("Repository ID cannot be null");
                
            var repository = _repositoryRepository.GetById(commit.RepositoryId.Value);
            if (repository == null)
                throw new ArgumentException("Repository not found");

            // Set commit date
            commit.CommitDate = DateTime.Now;
            // Create commit
            _commitRepository.Add(commit);
            _commitRepository.Save();
            
            // Update repository updated date
            repository.UpdatedDate = DateTime.Now;
            _repositoryRepository.Update(repository);
            _repositoryRepository.Save();
        }

        public Repository GetRepositoryByName(string owner, string repositoryName)
        {
            // This method needs to work with the repository pattern
            // Search through repositories with owners included
            var repositories = _repositoryRepository.GetAllWithOwners();
            
            // Log repository count for debugging
            _logger.LogInformation($"Searching for repository {owner}/{repositoryName} among {repositories.Count()} repositories");
            
            // Debug log for available repositories
            foreach (var repo in repositories.Take(10))
            {
                string ownerUsername = repo.Owner?.Username;
                string ownerGiteaUsername = repo.Owner?.GiteaUsername;
                
                _logger.LogDebug($"Repository: {repo.RepositoryName}, " +
                                $"Owner: {ownerUsername ?? "null"}, " +
                                $"Owner.GiteaUsername: {ownerGiteaUsername ?? "null"}, " + 
                                $"Is Match Username: {ownerUsername == owner}, " +
                                $"Is Match GiteaUsername: {ownerGiteaUsername == owner}, " +
                                $"Is Match RepoName: {repo.RepositoryName.Equals(repositoryName, StringComparison.OrdinalIgnoreCase)}");
            }
            
            if (repositories.Count() > 10)
            {
                _logger.LogDebug($"... and {repositories.Count() - 10} more repositories");
            }
            
            // Find the repository matching the owner username and repository name
            var result = repositories.FirstOrDefault(r => 
                r.Owner != null && 
                (r.Owner.Username == owner || r.Owner.GiteaUsername == owner) && 
                r.RepositoryName.Equals(repositoryName, StringComparison.OrdinalIgnoreCase));
            
            if (result != null)
            {
                _logger.LogInformation($"Found repository {owner}/{repositoryName} with ID {result.RepositoryId}");
            }
            else
            {
                _logger.LogWarning($"Repository {owner}/{repositoryName} not found in database");
            }
            
            return result;
        }
    }
}