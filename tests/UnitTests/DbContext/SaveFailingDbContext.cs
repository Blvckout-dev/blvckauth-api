using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Blvckout.BlvckAuth.API.Database;
using Blvckout.BlvckAuth.API.Settings;

namespace Blvckout.BlvckAuth.API.Tests.UnitTests.DbContext;

public class SaveFailingDbContext(
    DbContextOptions<AuthContext> options,
    IOptionsMonitor<DatabaseSettings> databaseSettings
) : AuthContext(options, databaseSettings)
{
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new DbUpdateException();
    }
}