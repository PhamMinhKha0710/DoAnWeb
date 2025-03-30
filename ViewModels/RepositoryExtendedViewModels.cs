using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DoAnWeb.Models;
using DoAnWeb.GitIntegration;

namespace DoAnWeb.ViewModels
{
    /// <summary>
    /// View model for branch list in a repository
    /// </summary>
    public class BranchListViewModel
    {
        public string Owner { get; set; }
        public string RepositoryName { get; set; }
        public Models.Repository Repository { get; set; }
        public List<GitIntegration.Models.GiteaBranch> Branches { get; set; } = new List<GitIntegration.Models.GiteaBranch>();
        public string DefaultBranch { get; set; }
        public bool IsOwner { get; set; }
        public string CurrentBranch { get; set; }
    }

    /// <summary>
    /// View model for creating a new branch in a repository
    /// </summary>
    public class CreateBranchViewModel
    {
        public string Owner { get; set; }
        public string RepositoryName { get; set; }
        public Models.Repository Repository { get; set; }
        public string BranchName { get; set; }
        
        [Required(ErrorMessage = "Branch name is required")]
        [Display(Name = "New Branch Name")]
        [RegularExpression(@"^[a-zA-Z0-9\-_\.\/]+$", ErrorMessage = "Branch name can only contain letters, numbers, hyphens, underscores, periods, and forward slashes")]
        public string NewBranchName { get; set; }
        
        [Required(ErrorMessage = "Source branch is required")]
        [Display(Name = "Create from")]
        public string SourceBranch { get; set; }
        
        public List<GitIntegration.Models.GiteaBranch> AvailableBranches { get; set; } = new List<GitIntegration.Models.GiteaBranch>();
    }

    /// <summary>
    /// View model for browsing repository content
    /// </summary>
    public class BrowseContentViewModel
    {
        public string Owner { get; set; }
        public string RepositoryName { get; set; }
        public string Branch { get; set; }
        public string Path { get; set; }
        public List<GitIntegration.Models.GiteaContent> Contents { get; set; } = new List<GitIntegration.Models.GiteaContent>();
        public string ParentPath { get; set; }
        public bool IsOwner { get; set; }
    }

    /// <summary>
    /// View model for commit history
    /// </summary>
    public class CommitHistoryViewModel
    {
        public string Owner { get; set; }
        public string RepositoryName { get; set; }
        public string Branch { get; set; }
        public List<GitIntegration.Models.GiteaCommit> Commits { get; set; } = new List<GitIntegration.Models.GiteaCommit>();
        public bool IsOwner { get; set; }
        public string Path { get; set; }
        public Models.Repository Repository { get; set; }
    }

    /// <summary>
    /// View model for creating a new file
    /// </summary>
    public class CreateFileViewModel
    {
        public string Owner { get; set; }
        public string RepositoryName { get; set; }
        public string Branch { get; set; }
        public string ParentPath { get; set; }
        public Models.Repository Repository { get; set; }
        public string TargetDirectory { get; set; }
        
        [Required(ErrorMessage = "File name is required")]
        [Display(Name = "File Name")]
        [RegularExpression(@"^[a-zA-Z0-9\-_\.]+[a-zA-Z0-9\-_\.]$", ErrorMessage = "File name can only contain letters, numbers, hyphens, underscores, and periods")]
        public string FileName { get; set; }
        
        [Required(ErrorMessage = "Content is required")]
        [Display(Name = "Content")]
        public string Content { get; set; }
        
        [Required(ErrorMessage = "Commit message is required")]
        [Display(Name = "Commit Message")]
        public string CommitMessage { get; set; } = "Add new file";
    }

    /// <summary>
    /// View model for editing a file
    /// </summary>
    public class EditFileViewModel
    {
        public string Owner { get; set; }
        public string RepositoryName { get; set; }
        public string Branch { get; set; }
        public string Path { get; set; }
        public string FileName { get; set; }
        public Models.Repository Repository { get; set; }
        
        [Required(ErrorMessage = "Content is required")]
        [Display(Name = "Content")]
        public string Content { get; set; }
        
        [Required(ErrorMessage = "Commit message is required")]
        [Display(Name = "Commit Message")]
        public string CommitMessage { get; set; } = "Update file";
        
        public string ParentPath { get; set; }
        public string FileType { get; set; }
    }

    /// <summary>
    /// View model for uploading a file
    /// </summary>
    public class UploadFileViewModel
    {
        public string Owner { get; set; }
        public string RepositoryName { get; set; }
        public string Repository { get; set; }
        public string Branch { get; set; }
        public string Path { get; set; }
        public string TargetDirectory { get; set; }
        public string FilePath { get; set; }
        
        [Required(ErrorMessage = "You must select a file to upload")]
        [Display(Name = "File")]
        public Microsoft.AspNetCore.Http.IFormFile File { get; set; }
        
        [Required(ErrorMessage = "Commit message is required")]
        [Display(Name = "Commit Message")]
        public string CommitMessage { get; set; } = "Upload file";
    }

    /// <summary>
    /// View model for uploading a folder
    /// </summary>
    public class UploadFolderViewModel
    {
        public string Owner { get; set; }
        public string RepositoryName { get; set; }
        public string Repository { get; set; }
        public string Branch { get; set; }
        public string Path { get; set; }
        public string TargetDirectory { get; set; }
        
        [Required(ErrorMessage = "You must select at least one file to upload")]
        [Display(Name = "Files")]
        public List<Microsoft.AspNetCore.Http.IFormFile> Files { get; set; }
        
        [Required(ErrorMessage = "Commit message is required")]
        [Display(Name = "Commit Message")]
        public string CommitMessage { get; set; } = "Upload folder";
    }

    /// <summary>
    /// View model for linking a repository to Gitea
    /// </summary>
    public class RepositoryLinkGiteaViewModel
    {
        public string Owner { get; set; }
        public string RepositoryName { get; set; }
        public string ReturnUrl { get; set; }
        
        [Required(ErrorMessage = "Gitea username is required")]
        [Display(Name = "Gitea Username")]
        public string GiteaUsername { get; set; }
        
        [Required(ErrorMessage = "Gitea password is required")]
        [Display(Name = "Gitea Password")]
        public string GiteaPassword { get; set; }
    }
} 