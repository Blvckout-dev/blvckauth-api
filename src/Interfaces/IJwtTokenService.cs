namespace Blvckout.BlvckAuth.Interfaces;

public interface IJwtTokenService
{
    string? GenerateToken(string? username, string? role, IEnumerable<string?>? scopes = null);
}