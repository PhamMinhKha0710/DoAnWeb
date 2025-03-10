using DoAnWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace DoAnWeb.Repositories
{
    public class QuestionRepository : Repository<Question>, IQuestionRepository
    {
        public QuestionRepository(DevCommunityContext context) : base(context)
        {
        }

        public IEnumerable<Question> GetQuestionsWithUsers()
        {
            return _dbSet
                .Include(q => q.User)
                .Include(q => q.Answers)
                .ToList();
        }

        public Question GetQuestionWithDetails(int questionId)
        {
            return _dbSet
                .Include(q => q.User)
                .Include(q => q.Tags)
                .Include(q => q.Answers)
                    .ThenInclude(a => a.User)
                .FirstOrDefault(q => q.QuestionId == questionId);
        }

        public IEnumerable<Question> GetQuestionsByTag(string tagName)
        {
            return _dbSet
                .Include(q => q.User)
                .Include(q => q.Tags)
                .Where(q => q.Tags.Any(t => t.TagName == tagName))
                .ToList();
        }

        public IEnumerable<Question> GetQuestionsByUser(int userId)
        {
            return _dbSet
                .Include(q => q.User)
                .Include(q => q.Tags)
                .Where(q => q.UserId == userId)
                .ToList();
        }

        public IEnumerable<Question> SearchQuestions(string searchTerm)
        {
            return _dbSet
                .Include(q => q.User)
                .Include(q => q.Tags)
                .Where(q => q.Title.Contains(searchTerm) || q.Body.Contains(searchTerm))
                .ToList();
        }
    }
}