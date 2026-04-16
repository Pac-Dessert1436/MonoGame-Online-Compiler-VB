using Microsoft.AspNetCore.Mvc;
using webapp.Services;
using webapp.Models;

namespace webapp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(UserService userService, ILogger<AuthController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResult>> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Username, email, and password are required");
        }

        try
        {
            var user = await _userService.CreateUserAsync(request.Username, request.Email, request.Password);
            if (user == null)
            {
                return BadRequest("Username or email already exists");
            }

            return Ok(new AuthResult
            {
                Success = true,
                UserId = user.Id,
                Username = user.Username,
                Message = "Registration successful"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return StatusCode(500, "Registration failed");
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResult>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Email and password are required");
        }

        try
        {
            var user = await _userService.ValidateUserAsync(request.Email, request.Password);
            if (user == null)
            {
                return Unauthorized("Invalid email or password");
            }

            // In a real application, you would generate and return a JWT token here
            return Ok(new AuthResult
            {
                Success = true,
                UserId = user.Id,
                Username = user.Username,
                Message = "Login successful"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, "Login failed");
        }
    }
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResult
{
    public bool Success { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}