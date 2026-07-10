using System.Linq.Expressions;
using Cleaning.DAL.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;

namespace Cleaning.BLL.Tests;

internal sealed class InMemoryUnitOfWork : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repositories = [];

    public InMemoryUnitOfWork With<T>(List<T> entities) where T : class
    {
        _repositories[typeof(T)] = new InMemoryRepository<T>(entities);
        return this;
    }

    public IGenericRepository<T> Repository<T>() where T : class
    {
        if (!_repositories.TryGetValue(typeof(T), out var repository))
        {
            repository = new InMemoryRepository<T>([]);
            _repositories[typeof(T)] = repository;
        }
        return (IGenericRepository<T>)repository;
    }

    public Task<int> SaveChangesAsync() => Task.FromResult(0);

    public Task<IDbContextTransaction> BeginTransactionAsync() =>
        Task.FromResult(new Mock<IDbContextTransaction>().Object);

    public Task<IDbContextTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel) =>
        Task.FromResult(new Mock<IDbContextTransaction>().Object);

    public void Dispose()
    {
    }
}

internal sealed class InMemoryRepository<T>(List<T> entities) : IGenericRepository<T> where T : class
{
    public Task<T?> GetByIdAsync(object id)
    {
        var idProperty = typeof(T).GetProperty("Id") ?? typeof(T).GetProperty("UserId");
        return Task.FromResult(entities.SingleOrDefault(item => Equals(idProperty?.GetValue(item), id)));
    }

    public Task<IEnumerable<T>> GetAllAsync() => Task.FromResult<IEnumerable<T>>(entities);

    public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression) =>
        Task.FromResult<IEnumerable<T>>(entities.Where(expression.Compile()).ToList());

    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> expression, string? includeProperties = null) =>
        Task.FromResult(entities.FirstOrDefault(expression.Compile()));

    public Task AddAsync(T entity)
    {
        entities.Add(entity);
        return Task.CompletedTask;
    }

    public void AddRange(IEnumerable<T> newEntities) => entities.AddRange(newEntities);

    public void Update(T entity)
    {
    }

    public void Remove(T entity) => entities.Remove(entity);

    public void RemoveRange(IEnumerable<T> removedEntities)
    {
        foreach (var entity in removedEntities.ToList()) entities.Remove(entity);
    }

    public Task<bool> ExistsAsync(Expression<Func<T, bool>> expression) =>
        Task.FromResult(entities.Any(expression.Compile()));

    public IQueryable<T> GetQueryable() => entities.AsQueryable();

    public Task<int> CountAsync(Expression<Func<T, bool>> expression) =>
 Task.FromResult(entities.Count(expression.Compile()));
}

