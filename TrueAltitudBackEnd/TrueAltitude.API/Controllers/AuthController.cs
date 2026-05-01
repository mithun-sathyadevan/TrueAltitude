using Microsoft.AspNetCore.Mvc;
using TrueAltitude.Application.DTOs;
using TrueAltitude.Application.Services;

namespace TrueAltitude.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.RegisterAsync(dto);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.LoginAsync(dto);

        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Login with Google token
    /// </summary>
    [HttpPost("login-google")]
    public async Task<IActionResult> LoginGoogle([FromBody] GoogleTokenDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Token))
        {
            return BadRequest(new { message = "Google token is required." });
        }

        var result = await _authService.LoginWithGoogleAsync(dto.Token);

        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }
}

public class GoogleTokenDto
{
    public string Token { get; set; } = string.Empty;
}
