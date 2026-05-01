using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
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
    Task<AuthResponseDto> VerifyEmailAsync(VerifyEmailDto dto);
    Task<AuthResponseDto> ResendOtpAsync(string email);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IGoogleOAuthService _googleOAuthService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    public AuthService(
        IUserRepository userRepository,
        IGoogleOAuthService googleOAuthService,
        IEmailService emailService,
        ILogger<AuthService> logger,
        string jwtSecret,
        string jwtIssuer,
        string jwtAudience)
    {
        _userRepository = userRepository;
        _googleOAuthService = googleOAuthService;
        _emailService = emailService;
        _logger = logger;
        _jwtSecret = jwtSecret;
        _jwtIssuer = jwtIssuer;
        _jwtAudience = jwtAudience;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.Name))
        {
            return new AuthResponseDto { Success = false, Message = "Email, password, and name are required." };
        }

        var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            if (existingUser.IsEmailVerified)
            {
                return new AuthResponseDto { Success = false, Message = "Email already registered." };
            }

            // Unverified account — refresh OTP and resend
            var freshOtp = GenerateOtp();
            existingUser.OtpCode = freshOtp;
            existingUser.OtpExpiresAt = DateTime.UtcNow.AddMinutes(10);
            await _userRepository.UpdateAsync(existingUser);

            QueueOtpEmail(existingUser.Email, existingUser.Name, freshOtp);

            return new AuthResponseDto
            {
                Success = true,
                Message = "A verification code has been resent to your email. Please verify to continue.",
                User = MapUserToDto(existingUser)
            };
        }

        var otpCode = GenerateOtp();

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            AvatarUrl = dto.AvatarUrl,
            Provider = "local",
            IsActive = true,
            IsEmailVerified = false,
            OtpCode = otpCode,
            OtpExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow
        };

        var createdUser = await _userRepository.CreateAsync(user);

        QueueOtpEmail(dto.Email, dto.Name, otpCode);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Registration successful. Please check your email for the verification code.",
            User = MapUserToDto(createdUser)
            // No token yet — must verify email first
        };
    }

    public async Task<AuthResponseDto> VerifyEmailAsync(VerifyEmailDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.OtpCode))
        {
            return new AuthResponseDto { Success = false, Message = "Email and OTP code are required." };
        }

        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null)
        {
            return new AuthResponseDto { Success = false, Message = "User not found." };
        }

        if (user.IsEmailVerified)
        {
            return new AuthResponseDto { Success = false, Message = "Email is already verified." };
        }

        if (user.OtpCode != dto.OtpCode)
        {
            return new AuthResponseDto { Success = false, Message = "Invalid verification code." };
        }

        if (user.OtpExpiresAt == null || user.OtpExpiresAt < DateTime.UtcNow)
        {
            return new AuthResponseDto { Success = false, Message = "Verification code has expired. Please request a new one." };
        }

        user.IsEmailVerified = true;
        user.OtpCode = null;
        user.OtpExpiresAt = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Email verified successfully.",
            Token = token,
            User = MapUserToDto(user)
        };
    }

    public async Task<AuthResponseDto> ResendOtpAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            return new AuthResponseDto { Success = false, Message = "User not found." };
        }

        if (user.IsEmailVerified)
        {
            return new AuthResponseDto { Success = false, Message = "Email is already verified." };
        }

        var otpCode = GenerateOtp();
        user.OtpCode = otpCode;
        user.OtpExpiresAt = DateTime.UtcNow.AddMinutes(10);
        await _userRepository.UpdateAsync(user);

        QueueOtpEmail(email, user.Name, otpCode);

        return new AuthResponseDto { Success = true, Message = "A new verification code has been sent to your email." };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return new AuthResponseDto { Success = false, Message = "Email and password are required." };
        }

        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return new AuthResponseDto { Success = false, Message = "Invalid email or password." };
        }

        if (!user.IsEmailVerified)
        {
            // Send a fresh OTP so they can verify immediately
            var freshOtp = GenerateOtp();
            user.OtpCode = freshOtp;
            user.OtpExpiresAt = DateTime.UtcNow.AddMinutes(10);
            await _userRepository.UpdateAsync(user);
            QueueOtpEmail(user.Email, user.Name, freshOtp);

            return new AuthResponseDto
            {
                Success = false,
                Message = "Email not verified. A new verification code has been sent to your email.",
                User = MapUserToDto(user)
            };
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Login successful.",
            Token = GenerateJwtToken(user),
            User = MapUserToDto(user)
        };
    }

    public async Task<AuthResponseDto> LoginWithGoogleAsync(string googleToken)
    {
        var googlePayload = await _googleOAuthService.ValidateAndParseTokenAsync(googleToken);
        if (googlePayload == null)
        {
            return new AuthResponseDto { Success = false, Message = "Invalid or expired Google token." };
        }

        var user = await _userRepository.GetByEmailAsync(googlePayload.Email);

        if (user == null)
        {
            user = new User
            {
                Name = googlePayload.Name,
                Email = googlePayload.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                AvatarUrl = googlePayload.Picture,
                Provider = "google",
                IsActive = true,
                IsEmailVerified = true, // Google accounts are pre-verified
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };
            user = await _userRepository.CreateAsync(user);
        }
        else
        {
            user.AvatarUrl = googlePayload.Picture;
            user.Provider = "google";
            user.IsEmailVerified = true;
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
        }

        return new AuthResponseDto
        {
            Success = true,
            Message = "Google login successful.",
            Token = GenerateJwtToken(user),
            User = MapUserToDto(user)
        };
    }

    private void QueueOtpEmail(string email, string name, string otpCode)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _emailService.SendOtpEmailAsync(email, name, otpCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {Email}", email);
            }
        });
    }

    private static string GenerateOtp()
    {
        return Random.Shared.Next(100000, 999999).ToString();
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

        return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
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
            IsEmailVerified = user.IsEmailVerified,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}
