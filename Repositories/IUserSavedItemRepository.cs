using DoAnWeb.Models;

namespace DoAnWeb.Repositories
{
    public interface IUserSavedItemRepository : IRepository<UserSavedItem>
    {
        // Get all saved items for a user
        IEnumerable<UserSavedItem> GetSavedItemsByUserId(int userId);
        
        // Get saved questions for a user
        IEnumerable<Question> GetSavedQuestionsByUserId(int userId);
        
        // Get saved answers for a user
        IEnumerable<Answer> GetSavedAnswersByUserId(int userId);
        
        // Check if an item is saved by a user
        bool IsItemSavedByUser(int userId, string itemType, int itemId);
        
        // Save an item for a user
        void SaveItem(int userId, string itemType, int itemId);
        
        // Remove a saved item for a user
        void RemoveSavedItem(int userId, string itemType, int itemId);
    }
}