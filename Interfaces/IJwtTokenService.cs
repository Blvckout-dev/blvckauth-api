namespace Bl4ckout.MyMasternode.Auth.Interfaces;

public interface IJwtTokenService
{
    Models.AuthenticationToken? GenerateToken(Models.Login login);
}