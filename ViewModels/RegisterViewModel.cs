using System.ComponentModel.DataAnnotations;

namespace DoAnWeb.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, ErrorMessage = "Username must be between 3 and 50 characters", MinimumLength = 3)]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100, ErrorMessage = "Email must be between 5 and 100 characters", MinimumLength = 5)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, ErrorMessage = "Password must be between 6 and 100 characters", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Display name is required")]
        [StringLength(100, ErrorMessage = "Display name must be between 3 and 100 characters", MinimumLength = 3)]
        public string DisplayName { get; set; }
    }
}