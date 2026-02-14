using System.Linq.Expressions;

using Domain.Repositories;

using Infrastructure.DataAccess;

using Microsoft.EntityFrameworkCore;

using Shared.Requests;
using Shared.Responses;

namespace Infrastructure.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        private readonly DbSet<T> _dbSet;
        private readonly DbContext _applicationDbContext;

        public BaseRepository(ApplicationDbContext context)
        {
            _applicationDbContext = context;
            _dbSet = _applicationDbContext.Set<T>();
        }

        public async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            return entity;
        }

        public async Task DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
        }

        public async Task<T> UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            return entity;
        }

        public async Task<PagedResponse<T>> GetAllAsync(
            IQueryable<T> query,
            PagedRequest pagedRequest)
        {
            var totalItems = await query.CountAsync();

            var items = await query
                .Skip(pagedRequest.PageSize * (pagedRequest.PageNumber - 1))
                .Take(pagedRequest.PageSize)
                .ToListAsync();

            return new PagedResponse<T>(items, pagedRequest.PageNumber, pagedRequest.PageSize, totalItems);
        }

        public async Task<PagedResponse<T>> GetAllAsync(
            PagedRequest pagedRequest)
        {
            var query = _dbSet.AsQueryable();

            var totalItems = await query.CountAsync();

            var items = await query
                .Skip(pagedRequest.PageSize * (pagedRequest.PageNumber - 1))
                .Take(pagedRequest.PageSize)
                .ToListAsync();

            return new PagedResponse<T>(items, pagedRequest.PageNumber, pagedRequest.PageSize, totalItems);
        }

        public async Task<List<T>> GetAllAsync(IQueryable<T> query)
        {
            return await query.ToListAsync();
        }
        public async Task<List<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<T> GetByIdAsync(
            int id,
            Func<IQueryable<T>, IQueryable<T>>? includeProperties = null)
        {
            IQueryable<T> query = _dbSet;

            if (includeProperties != null)
            {
                query = includeProperties(query);
            }

            return await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
        }

        

        public async Task SaveChangesAsync()
        {
            await _applicationDbContext.SaveChangesAsync();
        }

        public IQueryable<T> AsQueryable()
        {
            return _dbSet.AsQueryable();
        }

        public async Task<T> GetByCustomConditionAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }
    }
}
