using System.Linq.Expressions;
using HouseRentalAPI.Data;
using HouseRentalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HouseRentalAPI.Repositories;

public class Repository<T>(AppDbContext context) : IRepository<T> where T : BaseEntity
{
    protected AppDbContext Context { get; } = context;
    protected DbSet<T> DbSet => Context.Set<T>();

    public virtual Task<T?> GetByIdAsync(int id) =>
        DbSet.FirstOrDefaultAsync(e => e.Id == id);

    public async Task<IReadOnlyList<T>> GetAllAsync() =>
        await DbSet.AsNoTracking().ToListAsync();

    public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
        await DbSet.AsNoTracking().Where(predicate).ToListAsync();

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate) =>
        DbSet.AnyAsync(predicate);

    public async Task AddAsync(T entity) =>
        await DbSet.AddAsync(entity);

    public void Update(T entity) =>
        DbSet.Update(entity);

    public void Remove(T entity) =>
        DbSet.Remove(entity);

    public Task<int> SaveChangesAsync() =>
        Context.SaveChangesAsync();
}
