using DoAnWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace DoAnWeb.Repositories
{
    public class PostRepository : Repository<Post>, IPostRepository
    {
        public PostRepository(DevCommunityContext context) : base(context)
        {
        }

        public IEnumerable<Post> GetPostsByUserId(int userId)
        {
            return _dbSet
                .Include(p => p.User)
                .Where(p => p.UserId == userId)
                .ToList();
        }

        public int GetPostCountByUserId(int userId)
        {
            return _dbSet.Count(p => p.UserId == userId);
        }

        public Post GetPostWithDetails(int postId)
        {
            return _dbSet
                .Include(p => p.User)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefault(p => p.PostId == postId);
        }
    }
} 