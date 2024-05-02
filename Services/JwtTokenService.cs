using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Bl4ckout.MyMasternode.Auth.Settings;

namespace Bl4ckout.MyMasternode.Auth.Services;

public class JwtTokenService : Interfaces.IJwtTokenService
{
    private readonly ILogger<JwtTokenService> _logger;
    private JwtSettings _jwtSettings;

    // Temp. replacement for db users
    private readonly List<Models.User> _users =
    [
        new("1", "admin", "damin", "Administrator" /*, new[] { "shoes.read" } */ ),
        new("2", "user01", "suer", "User" /*, new[] { "shoes.read" } */ )
    ];

    public JwtTokenService(ILogger<JwtTokenService> logger, IOptionsMonitor<JwtSettings> jwtSettings)
    {
        _logger = logger;

        _jwtSettings = jwtSettings.CurrentValue;
        jwtSettings.OnChange(option => {
            _jwtSettings = option;
        });
    }

    public Models.AuthenticationToken? GenerateToken(Models.Login login)
    {
        if (string.IsNullOrWhiteSpace(_jwtSettings.Key))
            return null; // ToDo:Log missing key and return null;

        Models.User? user = _users.FirstOrDefault(u => u.Username == login.Username && u.Password == login.Password);
        
        if (user == null)
            return null;

        SymmetricSecurityKey secretKey = new(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        SigningCredentials signingCredentials = new(secretKey, SecurityAlgorithms.HmacSha256);
        DateTime expirationTimeStamp = DateTime.Now.AddMinutes(20);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Name, user.Username),
            new("role", user.Role)/*,
            new Claim("scope", string.Join(" ", user.Scopes)) */
        };

        var tokenOptions = new JwtSecurityToken(
            issuer: "https://localhost:5002",
            claims: claims,
            expires: expirationTimeStamp,
            signingCredentials: signingCredentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenOptions);

        return new Models.AuthenticationToken(tokenString);
    }
}