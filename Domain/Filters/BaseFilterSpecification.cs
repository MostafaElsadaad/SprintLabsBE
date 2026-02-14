using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Filters
{
    public abstract class BaseFilterSpecification<T> : IFilterSpecification<T>
    {
        public abstract IQueryable<T> Apply(IQueryable<T> query);
    }
}
