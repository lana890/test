using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Infrastructure.Repositories
{
    public interface IBaseRepository<T,W> where T : class
    {

        Task<T> GetById(W id);

        Task<W> Create(T entity);            

        Task Delete(W Id);

        Task<bool> Update(T entity, W Id);
    }
}
