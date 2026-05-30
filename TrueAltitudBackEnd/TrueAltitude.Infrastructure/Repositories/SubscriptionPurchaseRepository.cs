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

    public async Task<(List<SubscriptionPurchase> Purchases, int Total)> GetPagedWithUserAsync(
        int page,
        int pageSize,
        string? searchQuery,
        string? status)
    {
        var query = _context.SubscriptionPurchases
            .Include(p => p.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var normalized = searchQuery.Trim().ToLowerInvariant();
            query = query.Where(p =>
                p.User.Name.ToLower().Contains(normalized)
                || p.User.Email.ToLower().Contains(normalized)
                || p.PlanCode.ToLower().Contains(normalized)
                || p.PlanName.ToLower().Contains(normalized)
                || (p.ProviderOrderId != null && p.ProviderOrderId.ToLower().Contains(normalized))
                || (p.ProviderPaymentId != null && p.ProviderPaymentId.ToLower().Contains(normalized)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToLowerInvariant();
            query = query.Where(p => p.Status.ToLower() == normalizedStatus);
        }

        var total = await query.CountAsync();
        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Max(pageSize, 1);
        var skip = (safePage - 1) * safePageSize;

        var purchases = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(safePageSize)
            .ToListAsync();

        return (purchases, total);
    }

    public async Task<SubscriptionPurchase> UpdateAsync(SubscriptionPurchase purchase)
    {
        _context.SubscriptionPurchases.Update(purchase);
        await _context.SaveChangesAsync();
        return purchase;
    }
}
