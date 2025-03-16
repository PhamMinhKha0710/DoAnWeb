using System.Linq.Expressions;
using DoAnWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace DoAnWeb.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly DevCommunityContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(DevCommunityContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public IEnumerable<T> GetAll()
        {
            return [.. _dbSet];
        }

        public IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
        {
            return _dbSet.Where(predicate).ToList();
        }

        public T GetById(int id)
        {
            return _dbSet.Find(id); 
        }

        public void Add(T entity)
        {
            _dbSet.Add(entity);
        }

        public void Update(T entity)
        {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }

        public void Delete(T entity)
        {
            if (_context.Entry(entity).State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }
            _dbSet.Remove(entity);
        }

        public void Delete(int id)
        {
            T entityToDelete = _dbSet.Find(id);
            if (entityToDelete != null)
            {
                Delete(entityToDelete);
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }
        
        /// <summary>
        /// Gets the database context
        /// </summary>
        /// <returns>The database context</returns>
        public DbContext GetContext()
        {
            return _context;
        }
    }
}