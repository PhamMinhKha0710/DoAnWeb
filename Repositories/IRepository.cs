using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace DoAnWeb.Repositories
{
    public interface IRepository<T> where T : class
    {
        // Get all entities
        IEnumerable<T> GetAll();
        
        // Get entities with filter
        IEnumerable<T> Find(Expression<Func<T, bool>> predicate);
        
        // Get entity by id
        T GetById(int id);
        
        // Add entity
        void Add(T entity);
        
        // Update entity
        void Update(T entity);
        
        // Delete entity
        void Delete(T entity);
        
        // Delete entity by id
        void Delete(int id);
        
        // Save changes
        void Save();
        
        /// <summary>
        /// Gets the database context
        /// </summary>
        /// <returns>The database context</returns>
        DbContext GetContext();

        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T> GetByIdAsync(int id);
        Task DeleteAsync(int id);
        Task SaveAsync();
    }
}