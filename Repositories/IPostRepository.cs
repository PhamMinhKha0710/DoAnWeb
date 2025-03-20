using DoAnWeb.Models;

namespace DoAnWeb.Repositories
{
    public interface IPostRepository : IRepository<Post>
    {
        // Get posts by user
        IEnumerable<Post> GetPostsByUserId(int userId);
        
        // Get post count by user
        int GetPostCountByUserId(int userId);
        
        // Get post with details
        Post GetPostWithDetails(int postId);
    }
} 