using System.Collections.Generic;
using DoAnWeb.Models;
using DoAnWeb.ViewModels;

namespace DoAnWeb.Services
{
    public interface IUserService
    {
        // Get all users
        IEnumerable<User> GetAllUsers();
        
        // Get user by id
        User GetUserById(int id);
        
        // Get user by username
        User GetUserByUsername(string username);
        
        // Get user by email
        User GetUserByEmail(string email);
        
        // Create user
        void CreateUser(User user, string password);
        
        // Update user
        void UpdateUser(User user);
        
        // Delete user
        void DeleteUser(int id);
        
        // Authenticate user
        User Authenticate(string username, string password);
        
        // Check if username exists
        bool UsernameExists(string username);
        
        // Check if email exists
        bool EmailExists(string email);
        
        // Change password for a user (returns true if successful)
        bool ChangePassword(int userId, string currentPassword, string newPassword);
        
        // Delete account with password verification (returns true if successful)
        bool DeleteAccount(int userId, string password);
        
        // Email verification methods
        bool VerifyEmail(string token);
        
        // Send verification email
        bool SendVerificationEmail(int userId);
        
        // Generate verification token for email
        string GenerateEmailVerificationToken(int userId);
        
        // New profile method
        ProfileViewModel GetUserProfile(int userId);
    }
}