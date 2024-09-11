using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Blvckout.MyMasternode.Auth.Database;

public static class DbInitializer
{
    public static void Initialize(MyMasternodeAuthDbContext context)
    {
        context.Database.EnsureDeleted();
        context.Database.Migrate();

        context.Roles.AddRange(
            [
                new () {
                    Id = 2,
                    Name = "Administrator"
                }
            ]
        );

        context.Scopes.AddRange(
            [
                new () {
                    Id = 1,
                    Name = "user.create"
                },
                new () {
                    Id = 2,
                    Name = "user.delete"
                }
            ]
        );

        PasswordHasher<Entities.User> pwh = new(
            Microsoft.Extensions.Options.Options.Create(
                new PasswordHasherOptions()
                {
                    CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3, // V3 uses PBKDF2 with HMAC-SHA256, 128-bit salt, 256-bit subkey, 10000 iterations.
                    IterationCount = 600000 // Increasing to 600k iterations, recommended by OWASP
                }
            )
        );

        context.Users.AddRange(
            [
                new () {
                    Id = 1,
                    Username = "TestUser",
                    Password = pwh.HashPassword(new(), "user"),
                    RoleId = 1
                },
                new () {
                    Id = 2,
                    Username = "TestAdmin",
                    Password = pwh.HashPassword(new(),"admin"),
                    RoleId = 2
                }
            ]
        );

        context.UsersScopes.AddRange([
            new() {
                UserId = 2,
                ScopeId = 1
            },
            new () {
                UserId = 2,
                ScopeId = 2
            }
        ]);

        context.SaveChanges();
    }
}