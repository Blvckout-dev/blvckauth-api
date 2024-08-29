using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Bl4ckout.MyMasternode.Auth.Interfaces;
using Bl4ckout.MyMasternode.Auth.Utilities;

namespace Bl4ckout.MyMasternode.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(ILogger<AuthController> logger, IJwtTokenService jwtTokenService, Database.MyMasternodeAuthDbContext myMasternodeAuthDbContext) : ControllerBase
{
    private readonly ILogger<AuthController> _logger = logger;
    private readonly IJwtTokenService _jwtTokenService = jwtTokenService;
    private readonly Database.MyMasternodeAuthDbContext _myMasternodeAuthDbContext = myMasternodeAuthDbContext;

    [HttpPost("Register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] Models.Login register)
    {
         _logger.LogInformation("{methodName} method called.", nameof(Register));

        if (string.IsNullOrWhiteSpace(register.Username) ||
            string.IsNullOrWhiteSpace(register.Password)
        )
            return BadRequest("Username and/or password can not be empty");

        _logger.LogInformation("Registration requested for user: {username}", register.Username);

        // Check if user exists
        Database.Entities.User? user = await _myMasternodeAuthDbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == register.Username.ToLower());

        if (user is not null)
        {
            _logger.LogWarning("User: {username} already exist", register.Username);
            return BadRequest("Registration failed. Username already exists.");
        }

        user = new() {
            Username = register.Username
        };

        // Verify password
        PasswordHasher<Database.Entities.User> pwh = new(
            Options.Create(
                new PasswordHasherOptions()
                {
                    CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3, // V3 uses PBKDF2 with HMAC-SHA256, 128-bit salt, 256-bit subkey, 10000 iterations.
                    IterationCount = 600_000 // Increasing to 600k iterations, recommended by OWASP
                }
            )
        );

        user.Password = pwh.HashPassword(user, register.Password);

        await _myMasternodeAuthDbContext.Users.AddAsync(user);
        
        try
        {
            await _myMasternodeAuthDbContext.SaveChangesAsync();

            _logger.LogInformation("User: {username} created successfully.", user.Username);
            return Created();
        }
        catch (Exception ex)
        {
            _logger.LogDebugWithObject("User object: {user}", user);
            _logger.LogError(ex, "Failed to save user: {username}", user.Username);
            return Problem("Failed to save user, please try again later on");
        }
    }

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
            return BadRequest("Username and/or password can not be empty");

        _logger.LogInformation("Token requested for user: {username}", login.Username);

        // Check if user exists
        Database.Entities.User? user = await _myMasternodeAuthDbContext.Users
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
        PasswordHasher<Database.Entities.User> pwh = new(
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
            return Problem("Token generation failed. Please try again later.");
        }
    }
}