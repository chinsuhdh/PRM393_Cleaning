namespace Cleaning.BLL.Constants;

public static class PostgresErrorCodes
{
    public const string UniqueViolation = "23505";
    public const string ForeignKeyViolation = "23503";
    public const string SerializationFailure = "40001";
}
