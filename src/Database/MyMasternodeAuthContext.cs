using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Bl4ckout.MyMasternode.Auth.Database.Entities;
using Bl4ckout.MyMasternode.Auth.Settings;

namespace Bl4ckout.MyMasternode.Auth.Database;

public class MyMasternodeAuthDbContext(
    DbContextOptions<MyMasternodeAuthDbContext> options,
    IOptionsMonitor<DatabaseSettings> databaseSettings
) : DbContext(options)
{
    private readonly IOptionsMonitor<DatabaseSettings> _databaseSettings = databaseSettings;

    // Suppress null reference warnings, becuase the DbContext base constructor ensures that all DbSet properties will get initialized
    // and null will never be observed on them.
    public DbSet<Role> Roles { get; set; } = null!;
    
    public DbSet<Scope> Scopes { get; set; } = null!;
    
    public DbSet<User> Users { get; set; } = null!;

    public DbSet<UserScope> UsersScopes { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseMySql(
                _databaseSettings.CurrentValue.ConnectionString,
                ServerVersion.AutoDetect(_databaseSettings.CurrentValue.ConnectionString),
                mysqlOptions =>
                {
                    mysqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 10,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null
                    );
                }
            );
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>().ToTable("roles");
        modelBuilder.Entity<Role>(entity => 
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.Name).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            
            entity.HasMany(r => r.Users)
                  .WithOne(u => u.Role);
        });

        // Seed default user role
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "User" }
        );

        modelBuilder.Entity<Scope>().ToTable("scopes");
        modelBuilder.Entity<Scope>(entity => 
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.Name).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            
            entity.HasMany(s => s.Users)
                  .WithMany(u => u.Scopes);
        });

        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<User>(entity => 
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.Username).IsRequired();
            entity.HasIndex(e => e.Username).IsUnique();

            entity.Property(e => e.Password).IsRequired();
            
            entity.Property(e => e.RoleId).IsRequired().HasDefaultValue(1);

            entity.HasOne(u => u.Role)
                  .WithMany(r => r.Users)
                  .HasForeignKey(u => u.RoleId);
            
            entity.HasMany(e => e.Scopes)
                  .WithMany(e => e.Users)
                  .UsingEntity<UserScope>();
        });

        modelBuilder.Entity<UserScope>().ToTable("users_scopes");
        modelBuilder.Entity<UserScope>(entity => 
        {
            entity.HasKey(us => new { us.UserId, us.ScopeId });
            
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.ScopeId).IsRequired();

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId);

            entity.HasOne(e => e.Scope)
                  .WithMany()
                  .HasForeignKey(e => e.ScopeId);
        });
    }
}