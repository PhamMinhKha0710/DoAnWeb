using DoAnWeb.Models;

namespace DoAnWeb.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        // Get user by username
        User GetByUsername(string username);
        
        // Get user by email
        User GetByEmail(string email);
        
        // Check if username exists
        bool UsernameExists(string username);
        
        // Check if email exists
        bool EmailExists(string email);
        
        // Get user with roles
        User GetUserWithRoles(int userId);
        
        // Override GetAll to include related collections
        new IEnumerable<User> GetAll();
    }
}