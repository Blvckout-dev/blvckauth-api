namespace Bl4ckout.MyMasternode.Auth.Interfaces;

public interface IJwtTokenService
{
    Models.AuthenticationToken GenerateToken(string? username, string? role, IEnumerable<string?>? scopes = null);
}