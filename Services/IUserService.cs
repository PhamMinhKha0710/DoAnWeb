using DoAnWeb.Models;

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
    }
}