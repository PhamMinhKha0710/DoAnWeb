using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DoAnWeb.GitIntegration;
using DoAnWeb.Models;

namespace DoAnWeb.ViewModels
{
    public class RepositoryListViewModel
    {
        public IEnumerable<Repository> Repositories { get; set; } = new List<Repository>();
        public List<RepositoryResponse> GiteaRepositories { get; set; } = new List<RepositoryResponse>();
        public string SearchTerm { get; set; }
    }

    public class RepositoryDetailsViewModel
    {
        public Repository Repository { get; set; }
        public bool IsGiteaRepository { get; set; }
        public string GiteaUsername { get; set; }
        public string RepositoryName { get; set; }
    }

    public class CreateRepositoryViewModel
    {
        [Required(ErrorMessage = "Repository name is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Repository name must be between 3 and 100 characters")]
        [RegularExpression(@"^[a-zA-Z0-9\-_\.]+$", ErrorMessage = "Repository name can only contain letters, numbers, hyphens, underscores, and periods")]
        [Display(Name = "Repository Name")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Private Repository")]
        public bool IsPrivate { get; set; }
    }

    public class MyRepositoriesViewModel
    {
        public IEnumerable<Repository> DevCommunityRepositories { get; set; } = new List<Repository>();
        public List<RepositoryResponse> GiteaRepositories { get; set; } = new List<RepositoryResponse>();
    }

    public class FileContentViewModel
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string Content { get; set; }
        public string Encoding { get; set; }
        public string Owner { get; set; }
        public string Repository { get; set; }
        public long Size { get; set; }
        public string HtmlUrl { get; set; }
    }
} 