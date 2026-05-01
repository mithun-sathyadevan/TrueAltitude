using TrueAltitude.Application.DTOs;

namespace TrueAltitude.Application.DTOs;

public class SubscriptionPlanDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int PriceInPaise { get; set; }
    public int DurationDays { get; set; }
    public string? Description { get; set; }
    public bool IsPopular { get; set; }
}

public class CreateSubscriptionOrderDto
{
    public string PlanCode { get; set; } = string.Empty;
}

public class SubscriptionOrderResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int PurchaseId { get; set; }
    public string? ProviderOrderId { get; set; }
    public string Currency { get; set; } = "INR";
    public int AmountInPaise { get; set; }
    public string? RazorpayKeyId { get; set; }
    public bool IsMockOrder { get; set; }
}

public class VerifySubscriptionPaymentDto
{
    public int PurchaseId { get; set; }
    public string ProviderOrderId { get; set; } = string.Empty;
    public string ProviderPaymentId { get; set; } = string.Empty;
    public string ProviderSignature { get; set; } = string.Empty;
}

public class SubscriptionPaymentResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public UserResponseDto? User { get; set; }
}
