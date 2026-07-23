namespace Cleaning.BLL.Common;

public static class PostgresExceptionHelpers
{
    public static bool IsSqlState(Exception? exception, string sqlState)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current.GetType().Name == "PostgresException" &&
                current.GetType().GetProperty("SqlState")?.GetValue(current)?.ToString() == sqlState)
                return true;
        }
        return false;
    }
}
