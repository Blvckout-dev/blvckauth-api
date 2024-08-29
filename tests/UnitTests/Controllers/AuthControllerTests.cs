using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Bl4ckout.MyMasternode.Auth.Controllers;
using Bl4ckout.MyMasternode.Auth.Interfaces;
using Bl4ckout.MyMasternode.Auth.Database;
using Bl4ckout.MyMasternode.Auth.Database.Entities;
using Bl4ckout.MyMasternode.Auth.Settings;
using Bl4ckout.MyMasternode.Auth.Models;

namespace Bl4ckout.MyMasternode.Auth.Tests.UnitTests.Controllers;

public class AuthControllerTests
{
    private readonly DbContextOptions<MyMasternodeAuthDbContext> _dbOptions;

    private readonly AuthController _controller;
    private readonly MyMasternodeAuthDbContext _context;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<ILogger<AuthController>> _mockLogger;

    public AuthControllerTests()
    {
        // Setup in-memory database context
        _dbOptions = new DbContextOptionsBuilder<MyMasternodeAuthDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        _context = new MyMasternodeAuthDbContext(_dbOptions, Mock.Of<IOptionsMonitor<DatabaseSettings>>());

        // Seed initial data
        SeedDatabase();

        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockLogger = new Mock<ILogger<AuthController>>();

        _controller = new AuthController(_mockLogger.Object, _mockJwtTokenService.Object, _context);
    }

    private void SeedDatabase()
    {
        // Seed roles
        if (_context.Roles.AsNoTracking().FirstOrDefault(r => r.Id == 1) is null)
        {
            var role = new Role { Id = 1, Name = "User" };
            _context.Roles.Add(role);
        }

        if (_context.Users.AsNoTracking().FirstOrDefault(u => u.Id == 1) is null)
        {
            // Seed users
            var user = new User
            {
                Id = 1,
                Username = "existinguser",
                Password = new Microsoft.AspNetCore.Identity.PasswordHasher<User>().HashPassword(new (), "password"), // Hashed password
                RoleId = 1
            };
            _context.Users.Add(user);
        }

        _context.SaveChanges();
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData(null, "")]
    [InlineData("onlyusername", null)]
    [InlineData(null, "onlypass")]
    [InlineData("", "")] // Both username and password are empty
    [InlineData("onlyusername", "")] // Empty password
    [InlineData("", "password")] // Empty username
    public async Task Register_ShouldReturnBadRequest_WhenInvalidInput(string username, string password)
    {
        // Arrange
        var registerModel = new Login(username, password);

        // Act
        var result = await _controller.Register(registerModel);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenUsernameAlreadyExists()
    {
        // Arrange
        var registerModel = new Login("existinguser", "password");

        // Act
        var result = await _controller.Register(registerModel);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Register_ShouldReturnCreated_WhenRegistrationSucceeds()
    {
        // Arrange
        var registerModel = new Login("newuser", "password");

        // Act
        var result = await _controller.Register(registerModel);

        // Assert
        Assert.IsType<CreatedResult>(result);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData(null, "")]
    [InlineData("onlyusername", null)]
    [InlineData(null, "onlypass")]
    [InlineData("", "")] // Both username and password are empty
    [InlineData("onlyusername", "")] // Empty password
    [InlineData("", "password")] // Empty username
    public async Task Login_ShouldReturnBadRequest_WhenInvalidInput(string username, string password)
    {
        // Arrange
        var loginModel = new Login(username, password);

        // Act
        var result = await _controller.Login(loginModel);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenUserDoesNotExist()
    {
        // Arrange
        var loginModel = new Login("nonexistentuser", "password");

        // Act
        var result = await _controller.Login(loginModel);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_ShouldReturnOkWithToken_WhenLoginSucceeds()
    {
        // Arrange
        var loginModel = new Login("existinguser", "password");

        _mockJwtTokenService.Setup(s => s.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                            .Returns("mocked-jwt-token");

        // Act
        var result = await _controller.Login(loginModel);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
}