using System.Linq.Expressions;

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
    }
}