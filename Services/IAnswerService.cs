using DoAnWeb.Models;

namespace DoAnWeb.Services
{
    public interface IAnswerService
    {
        // Get answer by id
        Answer GetAnswerById(int id);
        
        // Get answer with details
        Answer GetAnswerWithDetails(int id);
        
        // Get answers by question
        IEnumerable<Answer> GetAnswersByQuestion(int questionId);
        
        // Get answers by user
        IEnumerable<Answer> GetAnswersByUser(int userId);
        
        // Create answer
        void CreateAnswer(Answer answer);
        
        // Update answer
        void UpdateAnswer(Answer answer);
        
        // Delete answer
        void DeleteAnswer(int id);
        
        // Vote answer
        void VoteAnswer(int answerId, int userId, bool isUpvote);
        
        // Accept answer
        void AcceptAnswer(int answerId, int questionId, int userId);
    }
}