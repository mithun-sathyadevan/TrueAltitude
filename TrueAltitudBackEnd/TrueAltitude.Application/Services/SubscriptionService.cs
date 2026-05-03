using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TrueAltitude.Application.DTOs;
using TrueAltitude.Domain.Entities;
using TrueAltitude.Infrastructure.Interfaces;

namespace TrueAltitude.Application.Services;

public interface ISubscriptionService
{
    Task<List<SubscriptionPlanDto>> GetPlansAsync();
    Task<SubscriptionOrderResponseDto> CreateOrderAsync(int userId, CreateSubscriptionOrderDto dto);
    Task<SubscriptionPaymentResultDto> VerifyPaymentAsync(int userId, VerifySubscriptionPaymentDto dto);
}

public class SubscriptionService : ISubscriptionService
{
    private const string SubscriptionPlansKey = "subscription.plans";

    private readonly IUserRepository _userRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ISubscriptionPurchaseRepository _purchaseRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        IUserRepository userRepository,
        ISettingsRepository settingsRepository,
        ISubscriptionPurchaseRepository purchaseRepository,
        IJwtTokenService jwtTokenService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<SubscriptionService> logger)
    {
        _userRepository = userRepository;
        _settingsRepository = settingsRepository;
        _purchaseRepository = purchaseRepository;
        _jwtTokenService = jwtTokenService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<SubscriptionPlanDto>> GetPlansAsync()
    {
        var plans = await GetConfiguredPlansAsync();
        return plans.OrderBy(p => p.DurationDays).ToList();
    }

    public async Task<SubscriptionOrderResponseDto> CreateOrderAsync(int userId, CreateSubscriptionOrderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PlanCode))
        {
            return new SubscriptionOrderResponseDto { Success = false, Message = "Plan code is required." };
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return new SubscriptionOrderResponseDto { Success = false, Message = "User not found." };
        }

        var plans = await GetConfiguredPlansAsync();
        var plan = plans.FirstOrDefault(p => string.Equals(p.Code, dto.PlanCode, StringComparison.OrdinalIgnoreCase));
        if (plan == null)
        {
            return new SubscriptionOrderResponseDto { Success = false, Message = "Subscription plan not found." };
        }

        var purchase = new SubscriptionPurchase
        {
            UserId = userId,
            PlanCode = plan.Code,
            PlanName = plan.Name,
            DurationDays = plan.DurationDays,
            AmountInPaise = plan.PriceInPaise,
            Currency = "INR",
            Status = "pending",
            PaymentProvider = "razorpay",
            CreatedAt = DateTime.UtcNow
        };

        purchase = await _purchaseRepository.CreateAsync(purchase);

        var razorpayEnabled = bool.TryParse(_configuration["Razorpay:Enabled"], out var enabled) && enabled;
        var keyId = _configuration["Razorpay:KeyId"] ?? string.Empty;
        var keySecret = _configuration["Razorpay:KeySecret"] ?? string.Empty;

        var providerOrderId = string.Empty;
        var isMock = false;

        if (razorpayEnabled && !string.IsNullOrWhiteSpace(keyId) && !string.IsNullOrWhiteSpace(keySecret))
        {
            var orderResult = await CreateRazorpayOrderAsync(purchase, keyId, keySecret);
            if (!orderResult.Success)
            {
                purchase.Status = "failed";
                await _purchaseRepository.UpdateAsync(purchase);
                return new SubscriptionOrderResponseDto { Success = false, Message = orderResult.Message };
            }

            providerOrderId = orderResult.ProviderOrderId!;
        }
        else
        {
            // Razorpay disabled or keys unavailable: use mock order id for local/dev flow.
            providerOrderId = $"mock_{purchase.Id}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            isMock = true;
        }

        purchase.ProviderOrderId = providerOrderId;
        await _purchaseRepository.UpdateAsync(purchase);

        return new SubscriptionOrderResponseDto
        {
            Success = true,
            Message = isMock ? "Mock subscription order created." : "Subscription order created.",
            PurchaseId = purchase.Id,
            ProviderOrderId = providerOrderId,
            AmountInPaise = purchase.AmountInPaise,
            Currency = purchase.Currency,
            RazorpayKeyId = keyId,
            IsMockOrder = isMock
        };
    }

    public async Task<SubscriptionPaymentResultDto> VerifyPaymentAsync(int userId, VerifySubscriptionPaymentDto dto)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(dto.PurchaseId);
        if (purchase == null || purchase.UserId != userId)
        {
            return new SubscriptionPaymentResultDto { Success = false, Message = "Subscription order not found." };
        }

        if (!string.Equals(purchase.ProviderOrderId, dto.ProviderOrderId, StringComparison.Ordinal))
        {
            return new SubscriptionPaymentResultDto { Success = false, Message = "Order mismatch." };
        }

        if (string.Equals(purchase.Status, "paid", StringComparison.OrdinalIgnoreCase))
        {
            var existingUser = await _userRepository.GetByIdAsync(userId);
            if (existingUser == null)
            {
                return new SubscriptionPaymentResultDto { Success = false, Message = "User not found." };
            }

            return new SubscriptionPaymentResultDto
            {
                Success = true,
                Message = "Payment already verified.",
                Token = _jwtTokenService.GenerateToken(existingUser),
                User = MapUserToDto(existingUser)
            };
        }

        var isMock = dto.ProviderOrderId.StartsWith("mock_", StringComparison.OrdinalIgnoreCase);
        if (!isMock)
        {
            var keySecret = _configuration["Razorpay:KeySecret"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(keySecret))
            {
                return new SubscriptionPaymentResultDto { Success = false, Message = "Razorpay secret is not configured." };
            }

            var verified = VerifyRazorpaySignature(dto.ProviderOrderId, dto.ProviderPaymentId, dto.ProviderSignature, keySecret);
            if (!verified)
            {
                purchase.Status = "failed";
                await _purchaseRepository.UpdateAsync(purchase);
                return new SubscriptionPaymentResultDto { Success = false, Message = "Payment signature verification failed." };
            }
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return new SubscriptionPaymentResultDto { Success = false, Message = "User not found." };
        }

        var now = DateTime.UtcNow;
        var effectiveStart = user.SubscriptionExpiresAt.HasValue && user.SubscriptionExpiresAt.Value > now
            ? user.SubscriptionExpiresAt.Value
            : now;
        var effectiveEnd = effectiveStart.AddDays(purchase.DurationDays);

        purchase.Status = "paid";
        purchase.ProviderPaymentId = dto.ProviderPaymentId;
        purchase.ProviderSignature = dto.ProviderSignature;
        purchase.PaidAt = now;
        purchase.SubscriptionEndsAt = effectiveEnd;
        await _purchaseRepository.UpdateAsync(purchase);

        user.SubscriptionStatus = "active";
        user.SubscriptionPlanCode = purchase.PlanCode;
        user.SubscriptionPlanName = purchase.PlanName;
        user.SubscriptionStartedAt = now;
        user.SubscriptionExpiresAt = effectiveEnd;
        await _userRepository.UpdateAsync(user);

        return new SubscriptionPaymentResultDto
        {
            Success = true,
            Message = "Subscription activated successfully.",
            Token = _jwtTokenService.GenerateToken(user),
            User = MapUserToDto(user)
        };
    }

    private async Task<List<SubscriptionPlanDto>> GetConfiguredPlansAsync()
    {
        var setting = await _settingsRepository.GetByKeyAsync(SubscriptionPlansKey);
        if (setting == null)
        {
            return new List<SubscriptionPlanDto>();
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<SubscriptionSettingsModel>(setting.ValueJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return parsed?.Plans?.Where(p => !string.IsNullOrWhiteSpace(p.Code)).ToList() ?? new List<SubscriptionPlanDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse subscription settings JSON for key {Key}", SubscriptionPlansKey);
            return new List<SubscriptionPlanDto>();
        }
    }

    private async Task<(bool Success, string? ProviderOrderId, string Message)> CreateRazorpayOrderAsync(
        SubscriptionPurchase purchase,
        string keyId,
        string keySecret)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyId}:{keySecret}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            var payload = new
            {
                amount = purchase.AmountInPaise,
                currency = purchase.Currency,
                receipt = $"sub_{purchase.Id}",
                notes = new { userId = purchase.UserId.ToString(), planCode = purchase.PlanCode }
            };

            var url = _configuration["Razorpay:OrderApiUrl"] ?? "https://api.razorpay.com/v1/orders";
            using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Razorpay order create failed. Status={StatusCode} Body={Body}", (int)response.StatusCode, responseText);
                return (false, null, "Failed to create Razorpay order.");
            }

            using var document = JsonDocument.Parse(responseText);
            var orderId = document.RootElement.TryGetProperty("id", out var idValue) ? idValue.GetString() : null;
            if (string.IsNullOrWhiteSpace(orderId))
            {
                return (false, null, "Invalid Razorpay order response.");
            }

            return (true, orderId, "Order created.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Razorpay order creation failed for purchase {PurchaseId}", purchase.Id);
            return (false, null, "Failed to create Razorpay order.");
        }
    }

    private static bool VerifyRazorpaySignature(string orderId, string paymentId, string signature, string secret)
    {
        var payload = $"{orderId}|{paymentId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var generated = Convert.ToHexString(hash).ToLowerInvariant();

        return string.Equals(generated, signature?.Trim().ToLowerInvariant(), StringComparison.Ordinal);
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

    private sealed class SubscriptionSettingsModel
    {
        public List<SubscriptionPlanDto> Plans { get; set; } = new();
    }
}
