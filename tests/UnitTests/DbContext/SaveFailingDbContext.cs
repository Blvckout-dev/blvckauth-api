using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Bl4ckout.MyMasternode.Auth.Database;
using Bl4ckout.MyMasternode.Auth.Settings;

namespace Bl4ckout.MyMasternode.Auth.Tests.UnitTests.DbContext;

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