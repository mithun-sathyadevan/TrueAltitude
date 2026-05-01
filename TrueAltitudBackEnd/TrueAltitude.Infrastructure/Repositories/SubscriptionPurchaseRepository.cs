using Microsoft.EntityFrameworkCore;
using TrueAltitude.Domain.Entities;
using TrueAltitude.Infrastructure.Interfaces;
using TrueAltitude.Persistence.Data;

namespace TrueAltitude.Infrastructure.Repositories;

public class SubscriptionPurchaseRepository : ISubscriptionPurchaseRepository
{
    private readonly TrueAltitudeDbContext _context;

    public SubscriptionPurchaseRepository(TrueAltitudeDbContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionPurchase> CreateAsync(SubscriptionPurchase purchase)
    {
        await _context.SubscriptionPurchases.AddAsync(purchase);
        await _context.SaveChangesAsync();
        return purchase;
    }

    public async Task<SubscriptionPurchase?> GetByIdAsync(int id)
    {
        return await _context.SubscriptionPurchases.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<SubscriptionPurchase?> GetByProviderOrderIdAsync(string providerOrderId)
    {
        return await _context.SubscriptionPurchases.FirstOrDefaultAsync(p => p.ProviderOrderId == providerOrderId);
    }

    public async Task<SubscriptionPurchase> UpdateAsync(SubscriptionPurchase purchase)
    {
        _context.SubscriptionPurchases.Update(purchase);
        await _context.SaveChangesAsync();
        return purchase;
    }
}
