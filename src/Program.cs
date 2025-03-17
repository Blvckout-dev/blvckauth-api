using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Blvckout.BlvckAuth.API.Interfaces;
using Blvckout.BlvckAuth.API.Services;
using Blvckout.BlvckAuth.API.Settings;
using Blvckout.BlvckAuth.API.Database;

namespace Blvckout.BlvckAuth.API;

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
        builder.Services.AddOptions<DatabaseSettings>()
            .Bind(builder.Configuration.GetSection(DatabaseSettings.SECTION))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Add Jwt configuration to di
        builder.Services.AddOptions<JwtSettings>()
            .BindConfiguration(JwtSettings.SECTION)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Admni Settings
        builder.Services.AddOptions<AdminSettings>()
            .BindConfiguration(AdminSettings.SECTION)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true); 

        // Register AutoMapper
        builder.Services.AddAutoMapper(typeof(Program));
        
        builder.Services.AddControllers()
            .AddNewtonsoftJson();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c => {
            c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme()
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = 
                    $"JWT Authorization header using the {JwtBearerDefaults.AuthenticationScheme} scheme.{Environment.NewLine}" +
                    $"Enter '{JwtBearerDefaults.AuthenticationScheme}' [space] and then your token in the text input below.{Environment.NewLine}" +
                    $"Example: \"{JwtBearerDefaults.AuthenticationScheme} eyJhbGciOiJIUzI1N...\""
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                {
                    new OpenApiSecurityScheme {
                        Reference = new OpenApiReference {
                            Type = ReferenceType.SecurityScheme,
                            Id = JwtBearerDefaults.AuthenticationScheme
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        builder.Services.AddAuthentication(x => {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer();

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

        // Add mysql context
        builder.Services.AddDbContext<BlvckAuthApiDbContext>();

        // Add services
        builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
        builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();

        try
        {
            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<BlvckAuthApiDbContext>();
                var databaseSettings = services.GetRequiredService<IOptions<DatabaseSettings>>().Value;

                if (app.Environment.IsDevelopment())
                    if (databaseSettings.SeedData)
                        DbInitializer.Initialize(context);
                
                // Check for admin user
                var adminSettings = services.GetRequiredService<IOptions<AdminSettings>>().Value;
                AddOrUpdateAdminUser(app.Logger, context, adminSettings);
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
        catch (HostAbortedException)
        {}
        catch (OptionsValidationException ex)
        {
            using var loggerFactory = LoggerFactory.Create(logging =>
            {
                logging.AddConsole();
                logging.AddDebug();
            });

            var logger = loggerFactory.CreateLogger<Program>();

            logger.LogError(ex, ex.Message);

            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            using var loggerFactory = LoggerFactory.Create(logging =>
            {
                logging.AddConsole();
                logging.AddDebug();
            });

            var logger = loggerFactory.CreateLogger<Program>();

            logger.LogError(ex, "An unexpected error occurred during startup.");
            
            Environment.Exit(1);
        }
    }

    private static void AddOrUpdateAdminUser(ILogger logger, BlvckAuthApiDbContext database, AdminSettings adminSettings)
    {
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
            Database.Entities.User? user = database.Users
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
            var pwh = new Microsoft.AspNetCore.Identity.PasswordHasher<Database.Entities.User>(
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
                logger.LogInformation("Admin user has been created/updated.");
            else
                logger.LogInformation("Admin user already exists.");

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while trying to create/update the admin user.");
        }
        finally
        {
            // Clear sensitive information
            if (adminSettings is not null)
                adminSettings.Password = null;
        }
    }
}