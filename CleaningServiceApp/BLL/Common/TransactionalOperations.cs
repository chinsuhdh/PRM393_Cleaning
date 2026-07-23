using System.Data;
using Cleaning.DAL.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cleaning.BLL.Common;

public static class TransactionalOperations
{
    public static async Task<T> ExecuteInTransactionAsync<T>(
        this IUnitOfWork unitOfWork,
        ILogger logger,
        AppError failureError,
        Func<Task<T>> action,
        IsolationLevel isolationLevel = IsolationLevel.Serializable)
    {
        using var transaction = await unitOfWork.BeginTransactionAsync(isolationLevel);
        try
        {
            var result = await action();
            await transaction.CommitAsync();
            return result;
        }
        catch (AppException)
        {
            await transaction.RollbackAsync();
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Transactional operation failed: {ErrorCode}", failureError.Code);
            throw new AppException(failureError, ex);
        }
    }

    public static Task ExecuteInTransactionAsync(
        this IUnitOfWork unitOfWork,
        ILogger logger,
        AppError failureError,
        Func<Task> action,
        IsolationLevel isolationLevel = IsolationLevel.Serializable) =>
        unitOfWork.ExecuteInTransactionAsync<object?>(logger, failureError, async () =>
        {
            await action();
            return null;
        }, isolationLevel);
}
