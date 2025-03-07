using DoAnWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace DoAnWeb.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(DevCommunityContext context) : base(context)
        {
        }

        public User GetByUsername(string username)
        {
            return _dbSet.FirstOrDefault(u => u.Username == username);
        }

        public User GetByEmail(string email)
        {
            return _dbSet.FirstOrDefault(u => u.Email == email);
        }

        public bool UsernameExists(string username)
        {
            return _dbSet.Any(u => u.Username == username);
        }

        public bool EmailExists(string email)
        {
            return _dbSet.Any(u => u.Email == email);
        }

        // Get user with roles
        public User GetUserWithRoles(int userId)
        {
            return _dbSet
                .Include(u => u.Roles)
                .FirstOrDefault(u => u.UserId == userId);
        }
    }
}