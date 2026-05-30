namespace TrueAltitude.Domain.Entities;

public enum UserRole
{
    Admin = 0,
    Manager = 1,
    Customer = 2
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Provider { get; set; } = "local"; // local, google, etc.
    public UserRole Role { get; set; } = UserRole.Customer; // Default role
    public bool IsActive { get; set; } = true;
    public bool IsEmailVerified { get; set; } = false;
    public string? OtpCode { get; set; }
    public DateTime? OtpExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }

    public string SubscriptionStatus { get; set; } = "none"; // none, active, expired
    public string? SubscriptionPlanCode { get; set; }
    public string? SubscriptionPlanName { get; set; }
    public DateTime? SubscriptionStartedAt { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
}
