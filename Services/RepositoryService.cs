using DoAnWeb.Models;
using DoAnWeb.Repositories;

namespace DoAnWeb.Services
{
    public class RepositoryService : IRepositoryService
    {
        private readonly IRepositoryRepository _repositoryRepository;
        private readonly IRepository<RepositoryFile> _fileRepository;
        private readonly IRepository<RepositoryCommit> _commitRepository;

        public RepositoryService(
            IRepositoryRepository repositoryRepository,
            IRepository<RepositoryFile> fileRepository,
            IRepository<RepositoryCommit> commitRepository)
        {
            _repositoryRepository = repositoryRepository;
            _fileRepository = fileRepository;
            _commitRepository = commitRepository;
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
            return _repositoryRepository.GetRepositoryWithFiles(id);
        }

        public Repository GetRepositoryWithCommits(int id)
        {
            return _repositoryRepository.GetRepositoryWithCommits(id);
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
    }
}