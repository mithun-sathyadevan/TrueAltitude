using TrueAltitude.Domain.Entities;

namespace TrueAltitude.Infrastructure.Interfaces;

public interface ISettingsRepository
{
    Task<Setting?> GetByKeyAsync(string key);
    Task<Setting> UpsertAsync(string key, string valueJson);
}
