using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Filters
{
    public interface IFilterSpecification<T>
    {
        IQueryable<T> Apply(IQueryable<T> query);
    }
}
