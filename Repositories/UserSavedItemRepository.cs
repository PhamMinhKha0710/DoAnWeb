using DoAnWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace DoAnWeb.Repositories
{
    public class UserSavedItemRepository : Repository<UserSavedItem>, IUserSavedItemRepository
    {
        private new readonly DevCommunityContext _context;
        
        public UserSavedItemRepository(DevCommunityContext context) : base(context)
        {
            _context = context;
        }

        public IEnumerable<UserSavedItem> GetSavedItemsByUserId(int userId)
        {
            return _dbSet
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.SavedDate)
                .ToList();
        }

        public IEnumerable<Question> GetSavedQuestionsByUserId(int userId)
        {
            var savedQuestionIds = _dbSet
                .Where(s => s.UserId == userId && s.ItemType == "Question")
                .Select(s => s.ItemId)
                .ToList();

            return _context.Questions
                .Include(q => q.User)
                .Include(q => q.Tags)
                .Where(q => savedQuestionIds.Contains(q.QuestionId))
                .OrderByDescending(q => q.CreatedDate)
                .ToList();
        }

        public IEnumerable<Answer> GetSavedAnswersByUserId(int userId)
        {
            var savedAnswerIds = _dbSet
                .Where(s => s.UserId == userId && s.ItemType == "Answer")
                .Select(s => s.ItemId)
                .ToList();

            return _context.Answers
                .Include(a => a.User)
                .Include(a => a.Question)
                    .ThenInclude(q => q.Tags)
                .Where(a => savedAnswerIds.Contains(a.AnswerId))
                .OrderByDescending(a => a.CreatedDate)
                .ToList();
        }

        public bool IsItemSavedByUser(int userId, string itemType, int itemId)
        {
            return _dbSet.Any(s => 
                s.UserId == userId && 
                s.ItemType == itemType && 
                s.ItemId == itemId);
        }

        public void SaveItem(int userId, string itemType, int itemId)
        {
            // Check if the item is already saved
            if (IsItemSavedByUser(userId, itemType, itemId))
                return;

            // Create a new saved item
            var savedItem = new UserSavedItem
            {
                UserId = userId,
                ItemType = itemType,
                ItemId = itemId,
                SavedDate = DateTime.Now
            };

            // Add to database
            _dbSet.Add(savedItem);
            _context.SaveChanges();
        }

        public void RemoveSavedItem(int userId, string itemType, int itemId)
        {
            var savedItem = _dbSet.FirstOrDefault(s => 
                s.UserId == userId && 
                s.ItemType == itemType && 
                s.ItemId == itemId);

            if (savedItem != null)
            {
                _dbSet.Remove(savedItem);
                _context.SaveChanges();
            }
        }
    }
}