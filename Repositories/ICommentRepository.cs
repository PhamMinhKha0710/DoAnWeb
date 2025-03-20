using DoAnWeb.Models;

namespace DoAnWeb.Repositories
{
    public interface ICommentRepository : IRepository<Comment>
    {
        // Get comments by user
        IEnumerable<Comment> GetCommentsByUserId(int userId);
        
        // Get comment count by user
        int GetCommentCountByUserId(int userId);
        
        // Get comment with details
        Comment GetCommentWithDetails(int commentId);
    }
} 