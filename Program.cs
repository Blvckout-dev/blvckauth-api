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

        builder.Services.AddControllers();
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

        // Add mongodb context
        builder.Services.AddDbContext<MyMasternodeAuthDbContext>(
            options => {
                options.UseMySql(
                    databaseSettings?.ConnectionString,
                    ServerVersion.AutoDetect(databaseSettings?.ConnectionString)
                );
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            }
        );

        // Add services
        builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

        var app = builder.Build();

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