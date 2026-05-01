using Microsoft.EntityFrameworkCore;
using TrueAltitude.Domain.Entities;
using TrueAltitude.Infrastructure.Interfaces;
using TrueAltitude.Persistence.Data;

namespace TrueAltitude.Infrastructure.Repositories;

public class SettingsRepository : ISettingsRepository
{
    private readonly TrueAltitudeDbContext _context;

    public SettingsRepository(TrueAltitudeDbContext context)
    {
        _context = context;
    }

    public async Task<Setting?> GetByKeyAsync(string key)
    {
        return await _context.Settings.FirstOrDefaultAsync(s => s.Key == key);
    }

    public async Task<Setting> UpsertAsync(string key, string valueJson)
    {
        var existing = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key);
        if (existing == null)
        {
            existing = new Setting
            {
                Key = key,
                ValueJson = valueJson,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Settings.AddAsync(existing);
        }
        else
        {
            existing.ValueJson = valueJson;
            existing.UpdatedAt = DateTime.UtcNow;
            _context.Settings.Update(existing);
        }

        await _context.SaveChangesAsync();
        return existing;
    }
}
