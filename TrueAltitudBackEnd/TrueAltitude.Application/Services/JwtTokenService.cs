using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TrueAltitude.Domain.Entities;

namespace TrueAltitude.Application.Services;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    public JwtTokenService(string jwtSecret, string jwtIssuer, string jwtAudience)
    {
        _jwtSecret = jwtSecret;
        _jwtIssuer = jwtIssuer;
        _jwtAudience = jwtAudience;
    }

    public string GenerateToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSecret);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("AvatarUrl", user.AvatarUrl ?? string.Empty),
            new("subscription_status", user.SubscriptionStatus ?? "none"),
            new("subscription_plan", user.SubscriptionPlanCode ?? string.Empty),
            new("subscription_plan_name", user.SubscriptionPlanName ?? string.Empty)
        };

        if (user.SubscriptionExpiresAt.HasValue)
        {
            claims.Add(new Claim("subscription_expires_at", user.SubscriptionExpiresAt.Value.ToUniversalTime().ToString("O")));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(24),
            Issuer = _jwtIssuer,
            Audience = _jwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
    }
}
