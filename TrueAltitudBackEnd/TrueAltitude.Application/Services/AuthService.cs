using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
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
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IGoogleOAuthService _googleOAuthService;
    private readonly IEmailService _emailService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;
    private readonly int _refreshTokenExpiryMinutes;

    public AuthService(
        IUserRepository userRepository,
        IGoogleOAuthService googleOAuthService,
        IEmailService emailService,
        IJwtTokenService jwtTokenService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _googleOAuthService = googleOAuthService;
        _emailService = emailService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
        _refreshTokenExpiryMinutes = configuration.GetValue<int?>("JwtSettings:RefreshTokenExpiryMinutes") ?? 60;
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
        var refreshToken = GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddMinutes(_refreshTokenExpiryMinutes);
        await _userRepository.UpdateAsync(user);

        var token = _jwtTokenService.GenerateToken(user);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Email verified successfully.",
            Token = token,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = user.RefreshTokenExpiresAt,
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

        if (!user.IsActive)
        {
            return new AuthResponseDto { Success = false, Message = "Your account is deactivated. Please contact support." };
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
        user.RefreshToken = GenerateRefreshToken();
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddMinutes(_refreshTokenExpiryMinutes);
        await _userRepository.UpdateAsync(user);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Login successful.",
            Token = _jwtTokenService.GenerateToken(user),
            RefreshToken = user.RefreshToken,
            RefreshTokenExpiresAt = user.RefreshTokenExpiresAt,
            User = MapUserToDto(user)
        };
    }

    public async Task<AuthResponseDto> LoginWithGoogleAsync(string googleToken)
    {
        try
        {
            var googlePayload = await _googleOAuthService.ValidateAndParseTokenAsync(googleToken);
            if (googlePayload == null)
            {
                return new AuthResponseDto { Success = false, Message = "Invalid or expired Google token." };
            }

            var normalizedEmail = Truncate(googlePayload.Email?.Trim(), 255) ?? string.Empty;
            var normalizedName = Truncate(googlePayload.Name?.Trim(), 255);
            var normalizedAvatarUrl = Truncate(googlePayload.Picture?.Trim(), 500);

            var user = await _userRepository.GetByEmailAsync(normalizedEmail);

            var refreshToken = GenerateRefreshToken();
            var refreshTokenExpiresAt = DateTime.UtcNow.AddMinutes(_refreshTokenExpiryMinutes);

            if (user == null)
            {
                user = new User
                {
                    Name = string.IsNullOrWhiteSpace(normalizedName) ? "User" : normalizedName,
                    Email = normalizedEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    AvatarUrl = normalizedAvatarUrl,
                    Provider = "google",
                    IsActive = true,
                    IsEmailVerified = true, // Google accounts are pre-verified
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow,
                    RefreshToken = refreshToken,
                    RefreshTokenExpiresAt = refreshTokenExpiresAt
                };
                user = await _userRepository.CreateAsync(user);
            }
            else
            {
                if (!user.IsActive)
                {
                    return new AuthResponseDto { Success = false, Message = "Your account is deactivated. Please contact support." };
                }

                user.Name = string.IsNullOrWhiteSpace(normalizedName) ? user.Name : normalizedName;
                user.AvatarUrl = normalizedAvatarUrl;
                user.Provider = "google";
                user.IsEmailVerified = true;
                user.LastLoginAt = DateTime.UtcNow;
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiresAt = refreshTokenExpiresAt;
                await _userRepository.UpdateAsync(user);
            }

            return new AuthResponseDto
            {
                Success = true,
                Message = "Google login successful.",
                Token = _jwtTokenService.GenerateToken(user),
                RefreshToken = user.RefreshToken,
                RefreshTokenExpiresAt = user.RefreshTokenExpiresAt,
                User = MapUserToDto(user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google login failed while creating/updating local user profile.");
            var detail = ex.InnerException?.Message ?? ex.Message;
            return new AuthResponseDto
            {
                Success = false,
                Message = $"Google login failed while saving user profile. {detail}"
            };
        }
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
        {
            return new AuthResponseDto { Success = false, Message = "Refresh token is required." };
        }

        var user = await _userRepository.GetByRefreshTokenAsync(dto.RefreshToken);
        if (user == null)
        {
            return new AuthResponseDto { Success = false, Message = "Invalid refresh token." };
        }

        if (!user.RefreshTokenExpiresAt.HasValue || user.RefreshTokenExpiresAt <= DateTime.UtcNow)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiresAt = null;
            await _userRepository.UpdateAsync(user);
            return new AuthResponseDto { Success = false, Message = "Refresh token expired. Please login again." };
        }

        if (!user.IsActive)
        {
            return new AuthResponseDto { Success = false, Message = "User account is inactive." };
        }

        user.RefreshToken = GenerateRefreshToken();
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddMinutes(_refreshTokenExpiryMinutes);
        await _userRepository.UpdateAsync(user);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Token refreshed successfully.",
            Token = _jwtTokenService.GenerateToken(user),
            RefreshToken = user.RefreshToken,
            RefreshTokenExpiresAt = user.RefreshTokenExpiresAt,
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

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[48];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
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
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            SubscriptionStatus = user.SubscriptionStatus,
            SubscriptionPlanCode = user.SubscriptionPlanCode,
            SubscriptionPlanName = user.SubscriptionPlanName,
            SubscriptionStartedAt = user.SubscriptionStartedAt,
            SubscriptionExpiresAt = user.SubscriptionExpiresAt,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}
