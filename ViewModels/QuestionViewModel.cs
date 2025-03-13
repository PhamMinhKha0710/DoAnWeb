using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

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

        public int UserId { get; set; }
        public DateTime CreatedDate { get; set; }
        
        // File upload properties
        [Display(Name = "Attachments")]
        public List<IFormFile> Attachments { get; set; } = new List<IFormFile>();
        
        // Property to store uploaded image URLs for markdown editor
        public List<string> UploadedImageUrls { get; set; } = new List<string>();
    }
}