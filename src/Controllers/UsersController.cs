using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Bl4ckout.MyMasternode.Auth.Utilities;
using Bl4ckout.MyMasternode.DataModels.Auth.V1.DTOs.Users;
using AutoMapper;
using Newtonsoft.Json;

namespace Bl4ckout.MyMasternode.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(
    ILogger<UsersController> logger,
    Database.MyMasternodeAuthDbContext myMasternodeAuthDbContext,
    IMapper mapper
) : ControllerBase
{
    private readonly ILogger<UsersController> _logger = logger;
    private readonly Database.MyMasternodeAuthDbContext _myMasternodeAuthDbContext = myMasternodeAuthDbContext;
    private readonly IMapper _mapper = mapper;

    [HttpGet]
    [Authorize(Policy = "UserRead")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] bool includeScopeIds = false)
    {
        _logger.LogInformation("{methodName} method called.", nameof(List));

        // Get all users with their role and scopes from the database
        var dbquery = _myMasternodeAuthDbContext.Users
            .AsNoTracking();

        if (includeScopeIds)
            dbquery = dbquery.Include(users => users.Scopes);

        var dbUsers = await dbquery.ToListAsync();

        _logger.LogDebug("Successfully retrieved {dbUsersCount} users.", dbUsers.Count);
        _logger.LogDebugWithObject("Database users:\n{dbUserList}", dbUsers);

        // Mapping database users to a list of user dtos
        IEnumerable<object> dtoUsers;

        if (includeScopeIds)
            dtoUsers = _mapper.Map<IEnumerable<UserDto>>(dbUsers);
        else
            dtoUsers = _mapper.Map<IEnumerable<UserMinimalDto>>(dbUsers);
        
        _logger.LogDebugWithObject("Mapped users:\n{dtoUserList}", dtoUsers);

        _logger.LogInformation("Successfully received all users from database");

        return Ok(dtoUsers);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "UserRead")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Details(int id)
    {
        _logger.LogInformation("{methodName} method called.", nameof(Details));

        _logger.LogDebug("Trying to receive user with id: {id}.", id);
        var dbUser = await _myMasternodeAuthDbContext.Users
            .AsNoTracking()
            .Include(users => users.Role)
            .Include(users => users.Scopes)
            .FirstOrDefaultAsync(u => u.Id == id);
        
        _logger.LogDebugWithObject("Database User:\n{dbUser}", dbUser);

        if (dbUser is null)
        {
            _logger.LogWarning("Failed to find user with id: {id}.", id);
            return NotFound($"Failed to find a user with id: {id}.");
        }

        _logger.LogDebug("Successfully received user from database");

        // Mapping database user to user detail dto
        var dtoUser = _mapper.Map<UserDetailDto>(dbUser);

        _logger.LogDebugWithObject("Mapped user:\n{dtoUser}", dtoUser);

        _logger.LogInformation("Successfully received user with id: {id}.", dbUser.Id);

        return Ok(dtoUser);
    }

    [HttpPost]
    [Authorize(Policy = "UserCreate")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] UserCreateDto userCreateDto)
    {
        _logger.LogInformation("{methodName} method called.", nameof(Create));

        Dictionary<string, string> errors = [];

        // Check if provided userCreateDto is valid
        if (string.IsNullOrWhiteSpace(userCreateDto.Username))
            errors.Add(nameof(userCreateDto.Username), $"{nameof(userCreateDto.Username)} cannot be empty.");
        
        if (string.IsNullOrWhiteSpace(userCreateDto.Password))
            errors.Add(nameof(userCreateDto.Password), $"{nameof(userCreateDto.Password)} cannot be empty.");
        
        if (
            userCreateDto.RoleId is not null &&
            _myMasternodeAuthDbContext.Roles
                .AsNoTracking()
                .FirstOrDefault(r => r.Id == userCreateDto.RoleId) is null
        )            
            errors.Add(nameof(userCreateDto.RoleId), $"{nameof(userCreateDto.RoleId)} does not exist.");

        _logger.LogDebugWithObject("User create dto:\n{userCreateDto}", userCreateDto);

        if (errors.Count > 0)
        {
            _logger.LogWarning("{errors}", errors);
            return BadRequest(errors);
        }

        _logger.LogDebug("Successfully validated userCreateDto.");

        // Check if username already exists
        if (
            await _myMasternodeAuthDbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == userCreateDto.Username) is not null
        )
        {
            _logger.LogWarning("Username: {username} already exists.", userCreateDto.Username);
            return Conflict($"{nameof(userCreateDto.Username)} already exists.");
        }

        // Building new user to insert into the database
        Database.Models.User dbuser = new () {
            Username = userCreateDto.Username
        };
        
        dbuser.RoleId = userCreateDto.RoleId ?? dbuser.RoleId;

        // Hash password
        string passwordHash = new PasswordHasher<Database.Models.User>(
            Options.Create(
                new PasswordHasherOptions()
                {
                    // V3 uses PBKDF2 with HMAC-SHA256, 128-bit salt, 256-bit subkey, 10000 iterations.
                    CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
                    // Increasing to 600k iterations, recommended by OWASP
                    IterationCount = 600_000
                }
            )
        ).HashPassword(dbuser, userCreateDto.Password);

        dbuser.Password = passwordHash;

        _logger.LogDebugWithObject("New database user:\n{dbUser}", dbuser);

       // Saving new user to database
        await _myMasternodeAuthDbContext.Users.AddAsync(dbuser);
        if (await _myMasternodeAuthDbContext.SaveChangesAsync() == 0)
        {
            _logger.LogWarning("Failed to save user to database:\n{dbUser}", JsonConvert.SerializeObject(dbuser));
            return Problem("Internal server error while processing your request.");
        }

        _logger.LogInformation("Successfully created user with id: {id}", dbuser.Id);

        return CreatedAtAction(
            nameof(Details),
            new { id = dbuser.Id },
            _mapper.Map<UserDto>(dbuser)
        );
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = "UserWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] JsonPatchDocument<UserUpdateDto> userUpdateDto)
    {
        _logger.LogInformation("{methodName} method called.", nameof(Update));
        
        // Checking if user exists in database
        var dbUser = await _myMasternodeAuthDbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id);

        if (dbUser is null)
        {
            _logger.LogWarning("No user with id: {id} found.", id);
            return NotFound($"No user with id: {id} found.");
        }

        // Mapping database user to new UserUpdateDto
        var userToPatch = _mapper.Map<UserUpdateDto>(dbUser);
        
        // Applying JsonPatchDocument to newly created UserUpdateDto
        userUpdateDto.ApplyTo(userToPatch, ModelState);

        // Validating patched UserUpdateDto
        if (!TryValidateModel(userToPatch))
        {
            _logger.LogWarning("Validation failed for patched UserUpdateDto:\n{userUpdateDto}", userToPatch);
            return ValidationProblem(ModelState);
        }

        // Map the userToPatch back to the dbUser entity
        _mapper.Map(userToPatch, dbUser);

        // Save changes to database
        await _myMasternodeAuthDbContext.SaveChangesAsync();

        _logger.LogInformation("Successfully updated user with id: {id}", id);

        return NoContent();
    }

    [HttpPost("{id}/scopes")]
    [Authorize(Policy = "UserWrite")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddScopesToUser(int id, [FromBody] ICollection<int> scopeIds)
    {
        _logger.LogInformation("{methodName} method called.", nameof(AddScopesToUser));

        // Check if request contains any scopes at all
        if (scopeIds is null || scopeIds.Count == 0)
        {
            _logger.LogWarning("The request doesn't contain any scopes");
            return BadRequest("No scopes provided");
        }

        // Check if user exists in db
        var dbUser = await _myMasternodeAuthDbContext.Users
            .AsNoTracking()
            .Include(u => u.Scopes)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (dbUser is null)
        {
            _logger.LogWarning("No user with id: {id} found.", id);
            return NotFound("User not found");
        }

        // Comparing requested scopes with scopes available in the db
        var requestedDbScopes = await _myMasternodeAuthDbContext.Scopes
            .AsNoTracking()
            .Where(s => scopeIds.Contains(s.Id))
            .ToListAsync();

        // Checking if requested scopes differe from scopes in db, meaning invalid scopes where requested
        if (scopeIds.Count != requestedDbScopes.Count)
        {
            var invalidScopeIds = scopeIds.Except(requestedDbScopes.Select(s => s.Id));
            _logger.LogWarning("The request contained following invalid scopes: {missingScopes}", string.Join(", ", invalidScopeIds));
            return BadRequest($"The request contained following invalid scopes: {string.Join(", ", invalidScopeIds)}");
        }

        // Checking if scopes are already assigned
        if (dbUser.Scopes is not null && dbUser.Scopes.Count > 0)
        {
            var hsDbUserScopeIds = dbUser.Scopes.Select(s => s.Id).ToHashSet();
            var hsRequestedScopeIds = requestedDbScopes.Select(s => s.Id).ToHashSet();

            if (hsRequestedScopeIds.IsSubsetOf(hsDbUserScopeIds))
            {
                _logger.LogInformation("User with id {id} is already assigned to the requested scopes.", id);
                return Ok("User already has the requested scopes.");
            }
        }

        // Adding scopes to UsersScopes join table
        foreach (var requestedScope in requestedDbScopes)
        {
            await _myMasternodeAuthDbContext.UsersScopes.AddAsync(new Database.Models.UserScope() {
                UserId = dbUser.Id,
                ScopeId = requestedScope.Id
            });   
        }
        
        // Saving dbUser changes to database
        await _myMasternodeAuthDbContext.SaveChangesAsync();
        _logger.LogInformation("Scopes successfully added to user with id: {id}.", id);

        return Ok();
    }

    [HttpDelete("{id}/scopes")]
    [Authorize(Policy = "UserWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveScopesFromUser(int id, [FromBody] ICollection<int> scopeIds)
    {
        _logger.LogInformation("{methodName} method called.", nameof(RemoveScopesFromUser));

        // Check if request contains any scopes at all
        if (scopeIds is null || scopeIds.Count == 0)
        {
            _logger.LogWarning("The request doesn't contain any scopes");
            return BadRequest("No scopes provided");
        }

        // Check if user exists in db
        var dbUser = await _myMasternodeAuthDbContext.Users
            .AsNoTracking()
            .Include(u => u.Scopes)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (dbUser is null)
        {
            _logger.LogWarning("No user with id: {id} found.", id);
            return NotFound("User not found");
        }

        // Check if user has any scopes assigned at all
        if (dbUser.Scopes is null || dbUser.Scopes.Count == 0)
        {
            _logger.LogInformation("User doesn't have any scopes assigned.");
            return NoContent();
        }

        foreach (var scopeId in scopeIds)
        {
            var dbUserScope = await _myMasternodeAuthDbContext.UsersScopes
                .FirstOrDefaultAsync(us => 
                    us.UserId == dbUser.Id &&
                    us.ScopeId == scopeId
                );
            
            if (dbUserScope is null) 
            {
                _logger.LogDebug(
                    "Skipping the removal of ScopeId: {scopeId} from User with Id: {userId} since it doesn't exist.",
                    scopeId, dbUser.Id
                );
                continue;
            }

            _logger.LogDebug(
                "Removing ScopeId: {scopeId} from User with Id: {userId}.",
                scopeId, dbUser.Id
            );
            
            _myMasternodeAuthDbContext.UsersScopes.Remove(dbUserScope);   
        }

        // Save changes to database
        await _myMasternodeAuthDbContext.SaveChangesAsync();
        _logger.LogInformation("Scopes successfully removed from user with id: {id}.", id);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "UserDelete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        _logger.LogInformation("{methodName} method called.", nameof(Delete));

        var dbUser = await _myMasternodeAuthDbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id);

        // Check if user exists in db
        if (dbUser is null)
        {
            _logger.LogInformation("No user with id: {id} found.", id);
            return NotFound("User not found");
        }

        // Delete user from the database
        _myMasternodeAuthDbContext.Users.Remove(dbUser);
        await _myMasternodeAuthDbContext.SaveChangesAsync();

        _logger.LogInformation("User with id: {id} deleted successfully.", id);

        return NoContent();
    }
}