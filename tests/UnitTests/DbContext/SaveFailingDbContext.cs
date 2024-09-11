using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Blvckout.MyMasternode.Auth.Database;
using Blvckout.MyMasternode.Auth.Settings;

namespace Blvckout.MyMasternode.Auth.Tests.UnitTests.DbContext;

public class SaveFailingDbContext(
    DbContextOptions<MyMasternodeAuthDbContext> options,
    IOptionsMonitor<DatabaseSettings> databaseSettings
) : MyMasternodeAuthDbContext(options, databaseSettings)
{
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new DbUpdateException();
    }
}