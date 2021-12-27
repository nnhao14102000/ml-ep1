using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace SohatNotebook.DataService.IRepository
{
    public interface IGenericRepository<T> where T:class
    {
        // Get all entities
         Task<IEnumerable<T>> All();

        // Get specify entity based on Id
        Task<T> GetById(Guid id);

        // Create new
        Task<bool> Add(T entity);

        // Delete
        Task<bool> Delete(Guid id, string userId);

        // Update entity or add if it does not exist
        Task<bool> Upsert(T entity);
    }
}