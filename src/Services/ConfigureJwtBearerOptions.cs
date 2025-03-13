using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Blvckout.BlvckAuth.Settings;

public class ConfigureJwtBearerOptions(
    IOptionsMonitor<JwtSettings> jwtOptions
) : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly IOptionsMonitor<JwtSettings> _jwtOptions = jwtOptions;

    public void Configure(JwtBearerOptions options)
    {
        Configure(JwtBearerDefaults.AuthenticationScheme, options);
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        var jwtOptions = _jwtOptions.CurrentValue;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtOptions.Key))
        };

        _jwtOptions.OnChange(newJwtOptions =>
        {
            options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(newJwtOptions.Key));
            options.TokenValidationParameters.ValidIssuer = newJwtOptions.Issuer;
            options.TokenValidationParameters.ValidAudience = newJwtOptions.Audience;
        });
    }
}