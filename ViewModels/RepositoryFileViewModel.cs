using System.ComponentModel.DataAnnotations;

namespace DoAnWeb.ViewModels
{
    public class RepositoryFileViewModel
    {
        public int FileId { get; set; }

        public int RepositoryId { get; set; }

        [Required(ErrorMessage = "File path is required")]
        [StringLength(255, ErrorMessage = "File path must be between 1 and 255 characters", MinimumLength = 1)]
        [Display(Name = "File Path")]
        public required string FilePath { get; set; }

        [Required(ErrorMessage = "File content is required")]
        [Display(Name = "File Content")]
        public required string FileContent { get; set; }
    }
}