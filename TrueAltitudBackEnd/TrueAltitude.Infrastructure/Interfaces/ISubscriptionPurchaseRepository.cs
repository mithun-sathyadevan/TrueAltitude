using TrueAltitude.Domain.Entities;

namespace TrueAltitude.Infrastructure.Interfaces;

public interface ISubscriptionPurchaseRepository
{
    Task<SubscriptionPurchase> CreateAsync(SubscriptionPurchase purchase);
    Task<SubscriptionPurchase?> GetByIdAsync(int id);
    Task<SubscriptionPurchase?> GetByProviderOrderIdAsync(string providerOrderId);
    Task<(List<SubscriptionPurchase> Purchases, int Total)> GetPagedWithUserAsync(
        int page,
        int pageSize,
        string? searchQuery,
        string? status);
    Task<SubscriptionPurchase> UpdateAsync(SubscriptionPurchase purchase);
}
