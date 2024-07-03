using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Bl4ckout.MyMasternode.DataModels.Auth.V1.DTOs.Users;
using AutoMapper;

namespace Bl4ckout.MyMasternode.Auth.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController(ILogger<UsersController> logger, Database.MyMasternodeAuthDbContext myMasternodeAuthDbContext, IMapper mapper) : ControllerBase
{
    private readonly ILogger<UsersController> _logger = logger;
    private readonly Database.MyMasternodeAuthDbContext _myMasternodeAuthDbContext = myMasternodeAuthDbContext;
    private readonly IMapper _mapper = mapper;

    [HttpGet]
    [Authorize(Policy = "UserRead")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var dbUsers = await _myMasternodeAuthDbContext.Users
            .AsNoTracking()
            .Include(users => users.Role)
            .Include(users => users.Scopes)
            .ToListAsync();

        // Mapping dbUsers to a list of UserDtos
        var dtoUsers = dbUsers.Select(dbUser => new UserDto
        {
            Id = dbUser.Id,
            Username = dbUser.Username,
            Role = dbUser.Role?.Name ?? throw new InvalidOperationException("Role should not be null"),
            Scopes = dbUser.Scopes?.Select(s => s.Name)
        });

        return Ok(dtoUsers);
    }

    [HttpGet("{username}")]
    [Authorize(Policy = "UserRead")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Details(string username)
    {
        var dbUser = await _myMasternodeAuthDbContext.Users
            .AsNoTracking()
            .Include(users => users.Role)
            .Include(users => users.Scopes)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (dbUser is null)
            return NotFound();

        var dtoUser = new UserDto
        {
            Id = dbUser.Id,
            Username = dbUser.Username,
            Role = dbUser.Role?.Name ?? throw new InvalidOperationException("Role should not be null"),
            Scopes = dbUser.Scopes?.Select(s => s.Name)
        };

        return Ok(dtoUser);
    }

    [HttpPost]
    [Authorize(Policy = "UserCreate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] UserCreateDto userCreateDto)
    {
        Dictionary<string, string> errors = [];

        // Check if provided userCreateDto is valid
        if (string.IsNullOrWhiteSpace(userCreateDto.Username))
            errors.Add(nameof(userCreateDto.Username), $"{nameof(userCreateDto.Username)} cannot be empty");
        
        if (string.IsNullOrWhiteSpace(userCreateDto.Password))
            errors.Add(nameof(userCreateDto.Password), $"{nameof(userCreateDto.Password)} cannot be empty");
        
        if (userCreateDto.RoleId is not null && _myMasternodeAuthDbContext.Roles.AsNoTracking().FirstOrDefault(r => r.Id == userCreateDto.RoleId) is null)            
            errors.Add(nameof(userCreateDto.RoleId), $"{nameof(userCreateDto.RoleId)} does not exist");

        if (errors.Count > 0)
            return BadRequest(errors);

        // Check if username already exists
        if (
            await _myMasternodeAuthDbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == userCreateDto.Username) is not null
        )
            return Conflict($"{nameof(userCreateDto.Username)} already exists");

        Database.Models.User user = new () {
            Username = userCreateDto.Username
        };
        
        user.RoleId = userCreateDto.RoleId ?? user.RoleId;

        // Hash password
        string passwordHash = new PasswordHasher<Database.Models.User>(
            Options.Create(
                new PasswordHasherOptions()
                {
                    CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3, // V3 uses PBKDF2 with HMAC-SHA256, 128-bit salt, 256-bit subkey, 10000 iterations.
                    IterationCount = 600_000 // Increasing to 600k iterations, recommended by OWASP
                }
            )
        ).HashPassword(user, userCreateDto.Password);

        user.Password = passwordHash;

        // Save new user
        await _myMasternodeAuthDbContext.Users.AddAsync(user);
        await _myMasternodeAuthDbContext.SaveChangesAsync();

        return Ok();
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = "UserUpdate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] JsonPatchDocument<UserUpdateDto> userUpdateDto)
    {
        if (userUpdateDto is null)
            return BadRequest();
        
        // Check if user exists in db
        var dbUser = await _myMasternodeAuthDbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id);

        if (dbUser is null)
            return NotFound();

        var userToPatch = _mapper.Map<UserUpdateDto>(dbUser);
        
        // Apply the patch document to userToPatch
        userUpdateDto.ApplyTo(userToPatch, ModelState);

        if (!TryValidateModel(userToPatch))
            return ValidationProblem(ModelState);

        // Map the userToPatch back to the dbUser entity
        _mapper.Map(userToPatch, dbUser);

        await _myMasternodeAuthDbContext.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("{id}/scopes")]
    [Authorize(Policy = "UserUpdate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddScopeToUser(int id, [FromBody] ICollection<string> scopes)
    {
        // Check if request contains any scopes at all
        if (scopes is null || scopes.Count == 0)
            return BadRequest();

        // Check if user exists in db
        var dbUser = await _myMasternodeAuthDbContext.Users
            .AsNoTracking()
            .Include(u => u.Scopes)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (dbUser is null)
            return NotFound("User not found");

        // Compare requested scopes with scopes available in the db
        var requestedDbScopes = await _myMasternodeAuthDbContext.Scopes
            .AsNoTracking()
            .Where(s => scopes.Contains(s.Name))
            .ToListAsync();

        // Check if requested scopes differe from scopes in db, meaning invalid scopes where requested
        if (scopes.Count != requestedDbScopes.Count)
            return BadRequest("Request contains invalid scopes");

        // Check if scopes are already assigned
        if (dbUser.Scopes is not null && !requestedDbScopes.Except(dbUser.Scopes).Any())
            return Ok();

        // Add scopes to UsersScopes join table
        foreach (var requestedScope in requestedDbScopes)
        {
            await _myMasternodeAuthDbContext.UsersScopes.AddAsync(new Database.Models.UserScope() {
                UserId = dbUser.Id,
                ScopeId = requestedScope.Id
            });   
        }
        
        await _myMasternodeAuthDbContext.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("{id}/scopes")]
    [Authorize(Policy = "UserUpdate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveScopeFromUser(int id, [FromBody] List<string> scopes)
    {
        // Check if request contains any scopes at all
        if (scopes is null || scopes.Count == 0)
            return BadRequest();

        // Check if user exists in db
        var dbUser = await _myMasternodeAuthDbContext.Users
            .AsNoTracking()
            .Include(u => u.Scopes)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (dbUser is null)
            return NotFound("User not found");

        // Check if user has any assigned scopes
        if (dbUser.Scopes is null || dbUser.Scopes?.Count == 0)
            return NoContent();
        
        // Compare requested scopes with scopes available in the db
        var requestedDbScopes = await _myMasternodeAuthDbContext.Scopes
            .AsNoTracking()
            .Where(s => scopes.Contains(s.Name))
            .ToListAsync();
        
        // Check if scopes are required to be removed
        if (!requestedDbScopes.Intersect(dbUser.Scopes!).Any())
            return NoContent();

        // Remove scopes from UsersScopes join table
        foreach (var requestedScope in requestedDbScopes)
        {
            // Remove scope from UsersScopes join table
            var dbUserScope = await _myMasternodeAuthDbContext.UsersScopes
                .FirstOrDefaultAsync(us => 
                    us.UserId == dbUser.Id &&
                    us.ScopeId == requestedScope.Id
                );
            
            if (dbUserScope is null) {
                // Log
                continue;
            }
            
            _myMasternodeAuthDbContext.UsersScopes.Remove(dbUserScope);   
        }

        await _myMasternodeAuthDbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "UserDelete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var dbUser = await _myMasternodeAuthDbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id);

        if (dbUser is null)
            return NotFound();

        return NoContent();
    }
}