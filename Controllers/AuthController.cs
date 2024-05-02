using Bl4ckout.MyMasternode.Auth.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Bl4ckout.MyMasternode.Auth.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(ILogger<AuthController> logger, IJwtTokenService jwtTokenService) : ControllerBase
{
    private readonly ILogger<AuthController> _logger = logger;
    private readonly IJwtTokenService _jwtTokenService = jwtTokenService;

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Login([FromBody] Models.Login login)
    {
        Models.AuthenticationToken? authToken = _jwtTokenService.GenerateToken(login);

        return authToken is null ? Unauthorized() : Ok(authToken);
    }
}
