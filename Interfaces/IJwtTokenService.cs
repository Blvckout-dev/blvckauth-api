namespace Bl4ckout.MyMasternode.Auth.Interfaces;

public interface IJwtTokenService
{
    string? GenerateToken(string? username, string? role, IEnumerable<string?>? scopes = null);
}