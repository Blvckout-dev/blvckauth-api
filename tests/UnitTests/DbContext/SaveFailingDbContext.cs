using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Blvckout.BlvckAuth.Database;
using Blvckout.BlvckAuth.Settings;

namespace Blvckout.BlvckAuth.Tests.UnitTests.DbContext;

public class SaveFailingDbContext(
    DbContextOptions<BlvckAuthDbContext> options,
    IOptionsMonitor<DatabaseSettings> databaseSettings
) : BlvckAuthDbContext(options, databaseSettings)
{
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new DbUpdateException();
    }
}