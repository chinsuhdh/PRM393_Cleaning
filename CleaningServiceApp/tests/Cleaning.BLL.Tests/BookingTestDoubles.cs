using Cleaning.BLL.Features.Admin;
using Cleaning.BLL.Features.Ai;
using Cleaning.BLL.Features.Bookings;
using Cleaning.BLL.Features.Chat;
using Cleaning.BLL.Features.Reviews;
using Cleaning.BLL.Features.ServiceCatalog;
using Cleaning.BLL.Features.UserAddresses;
using Cleaning.BLL.Features.Worker;
﻿using System.Linq.Expressions;
using AutoMapper;
using Cleaning.DAL.Interfaces;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Cleaning.BLL.Tests;

internal static class TestMapperFactory
{
    public static IMapper Create() =>
        new MapperConfiguration(configuration =>
        {
            configuration.AddProfile<BookingMappingProfile>();
            configuration.AddProfile<WorkerMappingProfile>();
            configuration.AddProfile<ReviewMappingProfile>();
            configuration.AddProfile<UserAddressMappingProfile>();
            configuration.AddProfile<ServiceMappingProfile>();
            configuration.AddProfile<AdminMappingProfile>();
            configuration.AddProfile<ChatMappingProfile>();
            configuration.AddProfile<AiMappingProfile>();
        }, NullLoggerFactory.Instance).CreateMapper();
}

internal sealed class TestAsyncQueryProvider<TEntity>(IQueryProvider inner) : IAsyncQueryProvider
{
    public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(expression);

    public object? Execute(Expression expression) => inner.Execute(expression);

    public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments().Single();
        var executionResult = typeof(IQueryProvider)
            .GetMethod(nameof(IQueryProvider.Execute), 1, [typeof(Expression)])!
            .MakeGenericMethod(resultType)
            .Invoke(inner, [expression]);

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, [executionResult])!;
    }
}

internal sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }

    public TestAsyncEnumerable(Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    public T Current => inner.Current;

    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(inner.MoveNext());

    public ValueTask DisposeAsync()
    {
        inner.Dispose();
        return ValueTask.CompletedTask;
    }
}

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

    public Task<IDbContextTransaction> BeginTransactionAsync() => Task.FromResult(CreateTransaction());

    public Task<IDbContextTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel) =>
        Task.FromResult(CreateTransaction());

    private static IDbContextTransaction CreateTransaction()
    {
        var mock = new Mock<IDbContextTransaction>();
        mock.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mock.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mock.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);
        return mock.Object;
    }

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

    public IQueryable<T> GetQueryable() => new TestAsyncEnumerable<T>(entities);

    // Đã sửa lỗi ở đây
    public Task<int> CountAsync(Expression<Func<T, bool>> expression) =>
        Task.FromResult(entities.Count(expression.Compile()));
}