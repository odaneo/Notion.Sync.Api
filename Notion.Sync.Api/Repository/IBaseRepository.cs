using System.Linq.Expressions;

namespace Notion.Sync.Api.Repository
{
    public interface IBaseRepository<T> where T : class
    {
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        Task<List<T>> FindAsync(Expression<Func<T, bool>> expression);
        Task<List<T>> FindAsNoTrackingAsync(Expression<Func<T, bool>> expression);
        Task<List<T>> GetAllAsync();
        IQueryable<T> GetQueryable();
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
        Task<bool> SaveAsync();
        void Update(T entity);
    }
}