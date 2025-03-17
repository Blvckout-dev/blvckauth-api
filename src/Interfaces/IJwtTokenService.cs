namespace Blvckout.BlvckAuth.API.Interfaces;

public interface IJwtTokenService
{
    string? GenerateToken(string? username, string? role, IEnumerable<string?>? scopes = null);
}