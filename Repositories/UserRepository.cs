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
            return _dbSet
                .Include(u => u.Questions)
                .Include(u => u.Answers)
                .Include(u => u.Votes)
                .AsSplitQuery()
                .FirstOrDefault(u => u.Username == username);
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
        
        public new virtual User GetById(int id)
        {
            return _dbSet
                .Include(u => u.Questions)
                    .ThenInclude(q => q.Tags)
                .Include(u => u.Answers)
                    .ThenInclude(a => a.Question)
                        .ThenInclude(q => q.Tags)
                .Include(u => u.Votes)
                .Include(u => u.Comments)
                .Include(u => u.Repositories)
                .AsSplitQuery()
                .FirstOrDefault(u => u.UserId == id);
        }

        // Get user with roles
        public User GetUserWithRoles(int userId)
        {
            return _dbSet
                .Include(u => u.Roles)
                .FirstOrDefault(u => u.UserId == userId);
        }
        
        // Override GetAll to include related collections for user statistics
        public new IEnumerable<User> GetAll()
        {
            return _dbSet
                .Include(u => u.Questions)
                .Include(u => u.Answers)
                .AsSplitQuery()
                .ToList();
        }
    }
}