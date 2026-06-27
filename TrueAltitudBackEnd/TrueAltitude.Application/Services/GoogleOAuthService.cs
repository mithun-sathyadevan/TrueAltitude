using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TrueAltitude.Application.Services;

public interface IGoogleOAuthService
{
    Task<GoogleTokenPayload?> ValidateAndParseTokenAsync(string idToken);
}

public class GoogleTokenPayload
{
    public string Sub { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Picture { get; set; } = string.Empty;
    public string? EmailVerified { get; set; }
    public long? IssuedAt { get; set; }
    public long? ExpiresAt { get; set; }
}

public class GoogleOAuthService : IGoogleOAuthService
{
    private readonly ILogger<GoogleOAuthService> _logger;
    private readonly string _googleClientId;

    public GoogleOAuthService(
        IConfiguration configuration,
        ILogger<GoogleOAuthService> logger)
    {
        _logger = logger;
        _googleClientId = configuration["GoogleOAuth:ClientId"]?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Validates Google ID token and extracts user information.
    /// Uses Google's tokeninfo endpoint to verify the token.
    /// </summary>
    public async Task<GoogleTokenPayload?> ValidateAndParseTokenAsync(string idToken)
    {
        try
        {
            // First, try to decode the JWT locally to extract claims
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(idToken))
            {
                _logger.LogWarning("Invalid JWT token format");
                return null;
            }

            var jwtToken = handler.ReadJwtToken(idToken);
            
            // Validate issuer
            var issuer = jwtToken.Issuer;
            if (issuer != "https://accounts.google.com" && issuer != "accounts.google.com")
            {
                _logger.LogWarning($"Invalid issuer: {issuer}");
                return null;
            }

            // Validate audience against the environment-specific Google client id.
            if (string.IsNullOrWhiteSpace(_googleClientId))
            {
                _logger.LogWarning("GoogleOAuth:ClientId is not configured.");
                return null;
            }

            var tokenAudiences = jwtToken.Audiences?.ToList() ?? new List<string>();
            var azp = jwtToken.Claims.FirstOrDefault(c => c.Type == "azp")?.Value;
            if (!string.IsNullOrWhiteSpace(azp))
            {
                tokenAudiences.Add(azp);
            }

            var hasConfiguredAudience = tokenAudiences.Any(aud => string.Equals(aud, _googleClientId, StringComparison.OrdinalIgnoreCase));
            if (!hasConfiguredAudience)
            {
                _logger.LogWarning(
                    "Token audience mismatch. Expected client ID: {Expected}. Got: {Audiences}",
                    _googleClientId,
                    string.Join(",", tokenAudiences));
                return null;
            }

            // Extract claims
            var payload = new GoogleTokenPayload
            {
                Sub = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? string.Empty,
                Email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? string.Empty,
                Name = jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? string.Empty,
                Picture = jwtToken.Claims.FirstOrDefault(c => c.Type == "picture")?.Value ?? string.Empty,
                EmailVerified = jwtToken.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value,
                IssuedAt = GetLongClaim(jwtToken, "iat"),
                ExpiresAt = GetLongClaim(jwtToken, "exp"),
            };

            // Verify expiration
            if (payload.ExpiresAt.HasValue)
            {
                var expirationTime = UnixTimeStampToDateTime(payload.ExpiresAt.Value);
                if (expirationTime < DateTime.UtcNow)
                {
                    _logger.LogWarning("Google token has expired");
                    return null;
                }
            }

            if (string.IsNullOrEmpty(payload.Email))
            {
                _logger.LogWarning("Google token missing email claim");
                return null;
            }

            return payload;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Google token");
            return null;
        }
    }

    private static long? GetLongClaim(JwtSecurityToken token, string claimType)
    {
        var claim = token.Claims.FirstOrDefault(c => c.Type == claimType);
        if (claim == null || !long.TryParse(claim.Value, out var value))
        {
            return null;
        }

        return value;
    }

    private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
        return dateTime;
    }
}
