using Bl4ckout.MyMasternode.Auth.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Bl4ckout.MyMasternode.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(ILogger<AuthController> logger, IJwtTokenService jwtTokenService, Database.MyMasternodeAuthDbContext myMasternodeAuthDbContext) : ControllerBase
{
    private readonly ILogger<AuthController> _logger = logger;
    private readonly IJwtTokenService _jwtTokenService = jwtTokenService;
    private readonly Database.MyMasternodeAuthDbContext _myMasternodeAuthDbContext = myMasternodeAuthDbContext;

    [HttpPost("Login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] Models.Login login)
    {
         _logger.LogInformation("{methodName} method called.", nameof(Login));

        if (string.IsNullOrWhiteSpace(login.Username) ||
            string.IsNullOrWhiteSpace(login.Password)
        )
            return BadRequest();

        _logger.LogInformation("Token requested for user: {username}", login.Username);

        // Check if user exists
        Database.Models.User? user = await _myMasternodeAuthDbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .Include(user => user.Scopes)
            .FirstOrDefaultAsync(u => u.Username == login.Username);

        if (user is null)
        {
            _logger.LogWarning("User: {username} doesn't exist", login.Username);
            return Unauthorized("Authentication failed. Please check your credentials.");
        }

        // Verify password
        PasswordHasher<Database.Models.User> pwh = new(
            Options.Create(
                new PasswordHasherOptions()
                {
                    CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3, // V3 uses PBKDF2 with HMAC-SHA256, 128-bit salt, 256-bit subkey, 10000 iterations.
                    IterationCount = 600_000 // Increasing to 600k iterations, recommended by OWASP
                }
            )
        );

        switch (pwh.VerifyHashedPassword(user, user.Password, login.Password))
        {
            case PasswordVerificationResult.Failed:
            _logger.LogWarning("User: {username} authentication failed", user.Username);
            return Unauthorized("Authentication failed. Please check your credentials.");
            case PasswordVerificationResult.Success:
            _logger.LogInformation("User: {username} authenticated successfully", user.Username);
            break;
            case PasswordVerificationResult.SuccessRehashNeeded:
            _logger.LogInformation("User: {username} authenticated successfully, however the password was encoded using a deprecated algorithm and should be rehashed and updated.", user.Username);
            // ToDo: Implement RehashUserPasswordToLatestVersion()
            break;
        }
        
        string? token = _jwtTokenService.GenerateToken(
            user.Username,
            user.Role?.Name,
            user.Scopes?.Select(s => s.Name)
        );

        if (!string.IsNullOrWhiteSpace(token))
        {
            _logger.LogInformation("The jwt token generation for username: {username} succeeded", login.Username);
            return Ok(new { Token = token });
        }
        else
        {
            _logger.LogInformation("Token generation for username: {username} failed", login.Username);
            return Unauthorized("Token generation failed. Please try again later.");
        }
    }
}