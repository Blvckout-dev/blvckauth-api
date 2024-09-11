namespace Blvckout.MyMasternode.Auth.Interfaces;

public interface IJwtTokenService
{
    string? GenerateToken(string? username, string? role, IEnumerable<string?>? scopes = null);
}