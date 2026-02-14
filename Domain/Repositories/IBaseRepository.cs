using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Domain.Filters;

using Shared.Requests;
using Shared.Responses;

namespace Domain.Repositories
{
    public interface IBaseRepository<T> where T : class
    {
        Task<T> AddAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task<T?> GetByIdAsync(int id, Func<IQueryable<T>, IQueryable<T>>? includeProperties = null);
        Task<T> GetByCustomConditionAsync(Expression<Func<T, bool>> predicate);
        Task<PagedResponse<T>> GetAllAsync(IQueryable<T> query, PagedRequest pagedRequest);
        Task<List<T>> GetAllAsync(IQueryable<T> query); 
        Task<PagedResponse<T>> GetAllAsync(PagedRequest pagedRequest);
        Task<List<T>> GetAllAsync();
        Task SaveChangesAsync();
        IQueryable<T> AsQueryable();
    }
}
