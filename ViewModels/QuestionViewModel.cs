using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace DoAnWeb.ViewModels
{
    /// <summary>
    /// ViewModel for creating and displaying questions
    /// Handles data transfer between the question form and controller
    /// </summary>
    public class QuestionViewModel
    {
        /// <summary>
        /// Unique identifier for the question
        /// </summary>
        public int QuestionId { get; set; }
        public int Id { get; set; }

        /// <summary>
        /// The question title with validation
        /// - Required field
        /// - Must be between 5-255 characters
        /// </summary>
        [Required(ErrorMessage = "Title is required")]
        [StringLength(255, ErrorMessage = "Title must be between 5 and 255 characters", MinimumLength = 5)]
        public required string Title { get; set; }

        /// <summary>
        /// The main content of the question with Markdown support
        /// - Required field
        /// - Minimum length of 20 characters
        /// </summary>
        [Required(ErrorMessage = "Body is required")]
        [MinLength(20, ErrorMessage = "Body must be at least 20 characters")]
        public required string Body { get; set; }

        /// <summary>
        /// Comma-separated list of tags to categorize the question
        /// These will be parsed and stored as separate tag entities
        /// </summary>
        [Display(Name = "Tags (comma separated)")]
        public required string Tags { get; set; }

        /// <summary>
        /// ID of the user who created the question
        /// </summary>
        public int UserId { get; set; }
        
        /// <summary>
        /// Timestamp when the question was created
        /// </summary>
        public DateTime CreatedDate { get; set; }
        
        /// <summary>
        /// Collection of file attachments uploaded with the question
        /// Supports multiple file uploads
        /// </summary>
        [Display(Name = "Attachments")]
        public List<IFormFile> Attachments { get; set; } = new List<IFormFile>();
        
        /// <summary>
        /// Collection of image URLs that have been uploaded via the Markdown editor
        /// Used to track images pasted or uploaded directly into the editor
        /// </summary>
        public List<string> UploadedImageUrls { get; set; } = new List<string>();
        public bool IsSaved { get; set; } // Added property to track if question is saved by current user
    }
}