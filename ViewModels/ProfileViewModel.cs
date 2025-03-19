using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DoAnWeb.ViewModels
{
    public class ProfileViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, ErrorMessage = "Username must be between 3 and 50 characters", MinimumLength = 3)]
        public required string Username { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100, ErrorMessage = "Email must be between 5 and 100 characters", MinimumLength = 5)]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Display name is required")]
        [StringLength(100, ErrorMessage = "Display name must be between 3 and 100 characters", MinimumLength = 3)]
        public required string DisplayName { get; set; }

        [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters")]
        public string Bio { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Avatar URL cannot exceed 255 characters")]
        [Url(ErrorMessage = "Invalid URL format")]
        public string AvatarUrl { get; set; } = string.Empty;
        
        // New properties for file upload
        [Display(Name = "Profile Image")]
        public IFormFile? ProfileImage { get; set; }
        
        // Property to handle avatar removal
        public bool RemoveAvatar { get; set; } = false;
    }
}