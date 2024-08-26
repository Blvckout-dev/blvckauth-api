using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Bl4ckout.MyMasternode.Auth.Settings;

namespace Bl4ckout.MyMasternode.Auth.Services;

public class JwtTokenService(
    ILogger<JwtTokenService> logger,
    IOptionsMonitor<JwtSettings> jwtSettings
) : Interfaces.IJwtTokenService
{
    private readonly ILogger _logger = logger;
    private readonly IOptionsMonitor<JwtSettings> _jwtSettings = jwtSettings;

    private const string ROLE = "role";
    private const string SCOPE = "scope";

    public string? GenerateToken(string? username, string? role, IEnumerable<string?>? scopes = null)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(role))
        {
            _logger.LogWarning("Username: {username} and/or Role: {role}", username, role);
            return null;
        }
        
        SymmetricSecurityKey secretKey = new(System.Text.Encoding.UTF8.GetBytes(_jwtSettings.CurrentValue.Key!));
        SigningCredentials signingCredentials = new(secretKey, SecurityAlgorithms.HmacSha256);
        DateTime expirationTimeStamp = DateTime.Now.AddMinutes(20);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Name, username),
            new(ROLE, role)
        };

        if (scopes is not null)
            foreach (string? scope in scopes)
                if (!string.IsNullOrWhiteSpace(scope))
                    claims.Add(new Claim(SCOPE, scope));

        var tokenOptions = new JwtSecurityToken(
            issuer: _jwtSettings.CurrentValue.Issuer,
            audience: _jwtSettings.CurrentValue.Audience,
            claims: claims,
            expires: expirationTimeStamp,
            signingCredentials: signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(tokenOptions);
    }
}