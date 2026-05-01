using TrueAltitude.Domain.Entities;

namespace TrueAltitude.Infrastructure.Interfaces;

public interface ISubscriptionPurchaseRepository
{
    Task<SubscriptionPurchase> CreateAsync(SubscriptionPurchase purchase);
    Task<SubscriptionPurchase?> GetByIdAsync(int id);
    Task<SubscriptionPurchase?> GetByProviderOrderIdAsync(string providerOrderId);
    Task<SubscriptionPurchase> UpdateAsync(SubscriptionPurchase purchase);
}
