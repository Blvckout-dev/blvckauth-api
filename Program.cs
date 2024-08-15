using System.Text;
using Bl4ckout.MyMasternode.Auth.Interfaces;
using Bl4ckout.MyMasternode.Auth.Services;
using Bl4ckout.MyMasternode.Auth.Settings;
using Bl4ckout.MyMasternode.Auth.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

namespace Bl4ckout.MyMasternode.Auth;

class Program
{
    private const string SCOPE = "scope";
    private const string ROLE_ADMINISTRATOR = "Administrator";

    static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Read environment variables
        builder.Configuration.AddEnvironmentVariables();
        
        // Check configuration
        if (!IsConfigurationValid(builder.Configuration))
            Environment.Exit(1);

        builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
        builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true); 

        // Register AutoMapper
        builder.Services.AddAutoMapper(typeof(Program));
        
        builder.Services.AddControllers()
            .AddNewtonsoftJson();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddAuthentication(x => {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(x => {
            if (builder.Environment.IsDevelopment())
                x.RequireHttpsMetadata = false;
                
            x.TokenValidationParameters = new TokenValidationParameters()
            {
                ValidIssuer = "my-masternode-auth",
                ValidAudience = "my-masternode",
                IssuerSigningKey = new SymmetricSecurityKey(
                    // In IsConfigurationValid(), we ensure JwtSettings:Key is valid
                    Encoding.UTF8.GetBytes(builder.Configuration.GetValue<string>("JwtSettings:Key")!)
                ),
                ValidateIssuerSigningKey = true
            };
        });

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("UserRead", policy => 
                policy.RequireAssertion(context => 
                    context.User.IsInRole(ROLE_ADMINISTRATOR) ||
                    (
                        context.User.HasClaim(SCOPE, "user.read") ||
                        context.User.HasClaim(SCOPE, "user.write") ||
                        context.User.HasClaim(SCOPE, "user.create") ||
                        context.User.HasClaim(SCOPE, "user.delete")
                    )
                )
            )
            .AddPolicy("UserWrite", policy => 
                policy.RequireAssertion(context => 
                    context.User.IsInRole(ROLE_ADMINISTRATOR) ||
                    context.User.HasClaim(SCOPE, "user.write")
                )
            )
            .AddPolicy("UserCreate", policy => 
                policy.RequireAssertion(context => 
                    context.User.IsInRole(ROLE_ADMINISTRATOR) ||
                    context.User.HasClaim(SCOPE, "user.create")
                )
            )
            .AddPolicy("UserDelete", policy => 
                policy.RequireAssertion(context => 
                    context.User.IsInRole(ROLE_ADMINISTRATOR) ||
                    context.User.HasClaim(SCOPE, "user.delete")
                )
            );

        builder.Services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build()
            );

        // Configuration
        DatabaseSettings? databaseSettings = builder.Configuration.GetSection("Database").Get<DatabaseSettings>();

        // Add mysql context
        builder.Services.AddDbContext<MyMasternodeAuthDbContext>(
            options => {
                options.UseMySql(
                    databaseSettings?.ConnectionString,
                    ServerVersion.AutoDetect(databaseSettings?.ConnectionString),
                    mysqlOptions => {
                        mysqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 10,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null
                        );
                    }
                );
                options.EnableDetailedErrors(builder.Environment.IsDevelopment());
                options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
            }
        );

        // Add services
        builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

        var app = builder.Build();

        
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<MyMasternodeAuthDbContext>();

            if (app.Environment.IsDevelopment())
                if (databaseSettings?.SeedData ?? true)
                    DbInitializer.Initialize(context);
            
            // Check for admin user
            AddOrUpdateAdminUser(context, builder.Configuration.GetSection("Admin").Get<AdminSettings>());
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }

    private static void AddOrUpdateAdminUser(MyMasternodeAuthDbContext database, AdminSettings? adminSettings)
    {
        using var loggerFactory = LoggerFactory.Create(logging =>
        {
            logging.AddConsole();
            logging.AddDebug();
        });

        var logger = loggerFactory.CreateLogger<Program>();

        try
        {
            if (adminSettings is null || string.IsNullOrWhiteSpace(adminSettings.Username))
            {
                logger.LogWarning("No admin user defined!");
                return;
            }

            if (string.IsNullOrWhiteSpace(adminSettings.Password))
            {
                logger.LogWarning("Password is not allowed to be empty!");
                return;
            }

            // Check if the admin user exists, otherwise create it
            Database.Models.User? user = database.Users
                .FirstOrDefault(u => u.Username == adminSettings.Username);

            if (user is null)
            {
                user = new ()
                {
                    Username = adminSettings.Username,
                    RoleId = 2
                };

                database.Users.Add(user);
            }

            // Prepare password hasher
            var pwh = new Microsoft.AspNetCore.Identity.PasswordHasher<Database.Models.User>(
                Microsoft.Extensions.Options.Options.Create(
                    new Microsoft.AspNetCore.Identity.PasswordHasherOptions()
                    {
                        // V3 uses PBKDF2 with HMAC-SHA256, 128-bit salt, 256-bit subkey, 10000 iterations.
                        CompatibilityMode = Microsoft.AspNetCore.Identity.PasswordHasherCompatibilityMode.IdentityV3,
                        // Increasing to 600k iterations, recommended by OWASP
                        IterationCount = 600_000
                    }
                )
            );

            // Compare current password with provided and update if necessary
            if (user.Password is null ||
                pwh.VerifyHashedPassword(user, user.Password, adminSettings.Password) != Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success)
            {
                user.Password = pwh.HashPassword(user, adminSettings.Password);
            }
            
            if (database.SaveChanges() > 0)
                logger.LogInformation("Admin user has been created/updated");
            else
                logger.LogWarning("Failed to save new admin user!");

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while trying to create/update the admin user.");
            throw;
        }
        finally
        {
            // Clear sensitive information
            if (adminSettings is not null)
                adminSettings.Password = null;
        }
    }

    private static bool IsConfigurationValid(IConfiguration configuration)
    {
        using var loggerFactory = LoggerFactory.Create(logging =>
        {
            logging.AddConsole();
            logging.AddDebug();
        });

        var logger = loggerFactory.CreateLogger<Program>();

        DatabaseSettings? databaseSettings = configuration.GetSection("Database").Get<DatabaseSettings>();

        if (databaseSettings == null)
        {
            logger.LogError("[Configuration][Database] Missing");
            return false;
        }

        if (string.IsNullOrWhiteSpace(databaseSettings?.ConnectionString))
        {
            logger.LogError("[Configuration][Database] Missing ConnectionString");
            return false;
        }

        JwtSettings? jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();
        
        if (jwtSettings == null)
        {
            logger.LogError("[Configuration][Jwt] Missing");
            return false;
        }

        if (string.IsNullOrWhiteSpace(jwtSettings?.Key))
        {
            logger.LogError("[Configuration][Jwt] Missing Key");
            return false;
        }
        
        logger.LogInformation("Configuration validated successfully");
        return true;
    }
}