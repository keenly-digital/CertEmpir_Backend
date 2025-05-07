using CertEmpire.Data;
using CertEmpire.Interfaces;
using CertEmpire.Models.CommonModel;
using Microsoft.EntityFrameworkCore;

namespace CertEmpire.Services
{
    public class Repository<T>(ApplicationDbContext context) : IRepository<T> where T : AuditableBaseEntity
    {
        protected readonly ApplicationDbContext _context = context;
        protected readonly DbSet<T> _dbSet = context.Set<T>();

        public virtual async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();

        public virtual async Task<T?> GetByIdAsync(Guid id) => await _dbSet.FindAsync(id);

        public virtual async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }

        public virtual async Task DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public virtual async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}