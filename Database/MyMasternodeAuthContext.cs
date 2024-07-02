using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Bl4ckout.MyMasternode.Auth.Database;

public class MyMasternodeAuthDbContext(DbContextOptions<MyMasternodeAuthDbContext> options) : DbContext(options)
{
    // Suppress null reference warnings, becuase the DbContext base constructor ensures that all DbSet properties will get initialized
    // and null will never be observed on them.
    public DbSet<Models.Role> Roles { get; set; } = null!;
    
    public DbSet<Models.Scope> Scopes { get; set; } = null!;
    
    public DbSet<Models.User> Users { get; set; } = null!;

    public DbSet<Models.UserScope> UsersScopes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Models.Role>().ToTable("roles");
        modelBuilder.Entity<Models.Role>(entity => 
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.Name).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            
            entity.HasMany(r => r.Users)
                  .WithOne(u => u.Role);
        });

        // Seed default user role
        modelBuilder.Entity<Models.Role>().HasData(
            new Models.Role { Id = 1, Name = "User" }
        );

        modelBuilder.Entity<Models.Scope>().ToTable("scopes");
        modelBuilder.Entity<Models.Scope>(entity => 
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.Name).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            
            entity.HasMany(s => s.Users)
                  .WithMany(u => u.Scopes);
        });

        modelBuilder.Entity<Models.User>().ToTable("users");
        modelBuilder.Entity<Models.User>(entity => 
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
                  .UsingEntity<Models.UserScope>();
        });

        modelBuilder.Entity<Models.UserScope>().ToTable("users_scopes");
        modelBuilder.Entity<Models.UserScope>(entity => 
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