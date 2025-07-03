using Microsoft.EntityFrameworkCore;
using Notion.Sync.Api.Database;
using System.Linq.Expressions;

namespace Notion.Sync.Api.Repository
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected readonly AppDbContext _appDbContext;
        protected readonly DbSet<T> _dbSet;
        protected BaseRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
            _dbSet = _appDbContext.Set<T>();
        }
        public IQueryable<T> GetQueryable()
        {
            return _dbSet.AsQueryable();
        }
        public async Task<List<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }
        public async Task<List<T>> FindAsync(Expression<Func<T, bool>> expression)
        {
            return await _dbSet.Where(expression).ToListAsync();
        }
        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }
        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }
        public void RemoveRange(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
        }
        public async Task<bool> SaveAsync()
        {
            return await _appDbContext.SaveChangesAsync() >= 0;
        }
        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }
        public void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }
    }
}
