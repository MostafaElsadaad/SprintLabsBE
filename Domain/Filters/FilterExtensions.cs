using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Filters
{
    public static class FilterExtensions
    {
        public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, IEnumerable<IFilterSpecification<T>> filters)
        {
            foreach (var filter in filters)
            {
                query = filter.Apply(query);
            }
            return query;
        }
        public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, IFilterSpecification<T> filter)
        {
            return filter.Apply(query);
        }
    }
}
