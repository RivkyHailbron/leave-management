using Xunit;

namespace LeaveManagement.Tests;

public sealed class PostgresOnlyFactAttribute : FactAttribute
{
    public PostgresOnlyFactAttribute()
    {
        if (TestDatabase.UseSqlite)
            Skip = "Requires PostgreSQL row locks.";
    }
}
