using DoAnWeb.Models;

namespace DoAnWeb.Services
{
    public interface IQuestionService
    {
        // Get all questions
        IEnumerable<Question> GetAllQuestions();
        
        // Get questions with users
        IEnumerable<Question> GetQuestionsWithUsers();
        
        // Get question by id
        Question GetQuestionById(int id);
        
        // Get question with details
        Question GetQuestionWithDetails(int id);
        
        // Get questions by tag
        IEnumerable<Question> GetQuestionsByTag(string tagName);
        
        // Get questions by user
        IEnumerable<Question> GetQuestionsByUser(int userId);
        
        // Search questions
        IEnumerable<Question> SearchQuestions(string searchTerm);
        
        // Create question
        void CreateQuestion(Question question, List<string> tagNames);
        
        // Update question
        void UpdateQuestion(Question question, List<string> tagNames);
        
        // Delete question
        void DeleteQuestion(int id);
        
        // Vote question
        void VoteQuestion(int questionId, int userId, bool isUpvote);
    }
}