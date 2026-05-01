using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TrueAltitude.Application.DTOs;
using TrueAltitude.Domain.Entities;
using TrueAltitude.Infrastructure.Interfaces;

namespace TrueAltitude.Application.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterUserDto dto);
    Task<AuthResponseDto> LoginAsync(LoginUserDto dto);
    Task<AuthResponseDto> LoginWithGoogleAsync(string googleToken);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    public AuthService(IUserRepository userRepository, string jwtSecret, string jwtIssuer, string jwtAudience)
    {
        _userRepository = userRepository;
        _jwtSecret = jwtSecret;
        _jwtIssuer = jwtIssuer;
        _jwtAudience = jwtAudience;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto dto)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.Name))
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Email, password, and name are required."
            };
        }

        // Check if email already exists
        if (await _userRepository.EmailExistsAsync(dto.Email))
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Email already registered."
            };
        }

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        // Create user
        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = passwordHash,
            AvatarUrl = dto.AvatarUrl,
            Provider = "local",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createdUser = await _userRepository.CreateAsync(user);

        // Generate JWT
        var token = GenerateJwtToken(createdUser);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Registration successful.",
            Token = token,
            User = MapUserToDto(createdUser)
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginUserDto dto)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Email and password are required."
            };
        }

        // Find user by email
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Invalid email or password."
            };
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Invalid email or password."
            };
        }

        // Update last login time
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        // Generate JWT
        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Login successful.",
            Token = token,
            User = MapUserToDto(user)
        };
    }

    public async Task<AuthResponseDto> LoginWithGoogleAsync(string googleToken)
    {
        // In a real implementation, validate googleToken with Google API
        // For now, this is a placeholder
        return new AuthResponseDto
        {
            Success = false,
            Message = "Google authentication not yet implemented."
        };
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSecret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim("AvatarUrl", user.AvatarUrl ?? string.Empty)
            }),
            Expires = DateTime.UtcNow.AddHours(24),
            Issuer = _jwtIssuer,
            Audience = _jwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static UserResponseDto MapUserToDto(User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            Provider = user.Provider,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}
