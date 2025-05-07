using CertEmpire.Models.CommonModel;

namespace CertEmpire.Interfaces
{
    public interface IRepository<T> where T : AuditableBaseEntity
    {
        Task<T?> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task SaveChangesAsync();
    }
}