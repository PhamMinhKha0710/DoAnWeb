using System.ComponentModel.DataAnnotations;

namespace DoAnWeb.ViewModels
{
    public class QuestionViewModel
    {
        public int QuestionId { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(255, ErrorMessage = "Title must be between 5 and 255 characters", MinimumLength = 5)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Body is required")]
        [MinLength(20, ErrorMessage = "Body must be at least 20 characters")]
        public string Body { get; set; }

        [Display(Name = "Tags (comma separated)")]
        public string Tags { get; set; }
    }
}