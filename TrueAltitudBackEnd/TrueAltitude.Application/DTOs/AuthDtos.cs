namespace TrueAltitude.Application.DTOs;

public class RegisterUserDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

public class LoginUserDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class GoogleLoginDto
{
    public string Token { get; set; } = string.Empty;
}

public class VerifyEmailDto
{
    public string Email { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
}

public class ResendOtpDto
{
    public string Email { get; set; } = string.Empty;
}

public class UserResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Provider { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public string SubscriptionStatus { get; set; } = "none";
    public string? SubscriptionPlanCode { get; set; }
    public string? SubscriptionPlanName { get; set; }
    public DateTime? SubscriptionStartedAt { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class AuthResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public UserResponseDto? User { get; set; }
}
