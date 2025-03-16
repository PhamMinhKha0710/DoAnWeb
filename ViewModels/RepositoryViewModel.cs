using System.ComponentModel.DataAnnotations;

namespace DoAnWeb.ViewModels
{
    public class RepositoryViewModel
    {
        public int RepositoryId { get; set; }

        [Required(ErrorMessage = "Repository name is required")]
        [StringLength(100, ErrorMessage = "Repository name must be between 3 and 100 characters", MinimumLength = 3)]
        [Display(Name = "Repository Name")]
        public required string RepositoryName { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public required string Description { get; set; }

        [Required(ErrorMessage = "Visibility is required")]
        [Display(Name = "Visibility")]
        public string Visibility { get; set; } = "Public";
    }
}