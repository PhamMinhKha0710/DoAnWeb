using DoAnWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace DoAnWeb.Repositories
{
    public class CommentRepository : Repository<Comment>, ICommentRepository
    {
        public CommentRepository(DevCommunityContext context) : base(context)
        {
        }

        public IEnumerable<Comment> GetCommentsByUserId(int userId)
        {
            return _dbSet
                .Include(c => c.User)
                .Where(c => c.UserId == userId)
                .ToList();
        }

        public int GetCommentCountByUserId(int userId)
        {
            return _dbSet.Count(c => c.UserId == userId);
        }

        public Comment GetCommentWithDetails(int commentId)
        {
            return _dbSet
                .Include(c => c.User)
                .FirstOrDefault(c => c.CommentId == commentId);
        }
    }
} 