using DoAnWeb.Models;

namespace DoAnWeb.Repositories
{
    public interface IQuestionRepository : IRepository<Question>
    {
        // Get questions with user information
        IEnumerable<Question> GetQuestionsWithUsers();
        
        // Get question with details (user, tags, comments)
        Question GetQuestionWithDetails(int questionId);
        
        // Get questions by tag
        IEnumerable<Question> GetQuestionsByTag(string tagName);
        
        // Get questions by user
        IEnumerable<Question> GetQuestionsByUser(int userId);
        
        // Search questions by title or content
        IEnumerable<Question> SearchQuestions(string searchTerm);
    }
}