namespace TrueAltitude.Domain.Entities;

public class SubscriptionPurchase
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string PlanCode { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public int DurationDays { get; set; }

    public int AmountInPaise { get; set; }
    public string Currency { get; set; } = "INR";

    public string Status { get; set; } = "pending"; // pending, paid, failed
    public string PaymentProvider { get; set; } = "razorpay";

    public string? ProviderOrderId { get; set; }
    public string? ProviderPaymentId { get; set; }
    public string? ProviderSignature { get; set; }
    public string? FailureReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public DateTime? SubscriptionEndsAt { get; set; }
}
