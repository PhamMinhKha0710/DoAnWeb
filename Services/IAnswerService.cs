using DoAnWeb.Models;

namespace DoAnWeb.Services
{
    public interface IAnswerService
    {
        // Get answer by id
        Answer GetAnswerById(int id);
        Task<Answer> GetAnswerByIdAsync(int id);
        
        // Get answer with details
        Answer GetAnswerWithDetails(int id);
        Task<Answer> GetAnswerWithDetailsAsync(int id);
        
        // Get answers by question
        IEnumerable<Answer> GetAnswersByQuestion(int questionId);
        Task<IEnumerable<Answer>> GetAnswersByQuestionAsync(int questionId);
        
        // Get answers by user
        IEnumerable<Answer> GetAnswersByUser(int userId);
        Task<IEnumerable<Answer>> GetAnswersByUserAsync(int userId);
        
        // Create answer
        void CreateAnswer(Answer answer);
        Task CreateAnswerAsync(Answer answer);
        
        // Update answer
        void UpdateAnswer(Answer answer);
        Task UpdateAnswerAsync(Answer answer);
        
        // Delete answer
        void DeleteAnswer(int id);
        Task DeleteAnswerAsync(int id);
        
        // Vote answer
        void VoteAnswer(int answerId, int userId, bool isUpvote);
        Task VoteAnswerAsync(int answerId, int userId, bool isUpvote);
        
        // Accept answer
        void AcceptAnswer(int answerId, int questionId, int userId);
        Task AcceptAnswerAsync(int answerId, int questionId, int userId);
    }
}