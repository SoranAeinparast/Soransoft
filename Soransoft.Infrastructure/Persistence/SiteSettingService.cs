using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Soransoft.Application.Interfaces;
using Soransoft.Domain.Entities;

namespace Soransoft.Infrastructure.Persistence
{
    /// <summary>سرویس تنظیمات سایت با کش درون‌حافظه‌ای</summary>
    public class SiteSettingService : ISiteSettingService
    {
        private readonly SoransoftDbContext _db;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "site-settings";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        public SiteSettingService(SoransoftDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<string> GetAsync(string key, CancellationToken ct = default)
        {
            var all = await GetAllAsync(ct);
            return all.TryGetValue(key, out var value) ? value : string.Empty;
        }

        public async Task<IDictionary<string, string>> GetAllAsync(CancellationToken ct = default)
        {
            if (_cache.TryGetValue(CacheKey, out IDictionary<string, string>? cached) && cached is not null)
                return cached;

            var settings = await _db.SiteSettings.AsNoTracking().ToListAsync(ct);
            var dict = settings.ToDictionary(s => s.Key, s => s.Value);
            _cache.Set(CacheKey, dict, CacheDuration);
            return dict;
        }

        public async Task<IDictionary<string, SiteSetting>> GetAllEntitiesAsync(CancellationToken ct = default)
        {
            var settings = await _db.SiteSettings.OrderBy(s => s.Group).ThenBy(s => s.DisplayOrder).ToListAsync(ct);
            return settings.ToDictionary(s => s.Key, s => s);
        }

        public async Task UpdateAsync(IEnumerable<(string Key, string Value)> updates, CancellationToken ct = default)
        {
            var keys = updates.Select(u => u.Key).ToList();
            var entities = await _db.SiteSettings.Where(s => keys.Contains(s.Key)).ToListAsync(ct);
            foreach (var entity in entities)
            {
                var match = updates.FirstOrDefault(u => u.Key == entity.Key);
                if (match.Key is not null) entity.Value = match.Value;
            }
            await _db.SaveChangesAsync(ct);
            _cache.Remove(CacheKey);
        }

        public async Task SetAsync(string key, string value, CancellationToken ct = default)
        {
            var entity = await _db.SiteSettings.FirstOrDefaultAsync(s => s.Key == key, ct);
            if (entity is null)
            {
                _db.SiteSettings.Add(new SiteSetting
                {
                    Key = key,
                    Title = key,
                    Value = value,
                    Group = "قابلیت‌ها",
                    Type = "boolean",
                    DisplayOrder = 1000,
                });
            }
            else
            {
                entity.Value = value;
            }

            await _db.SaveChangesAsync(ct);
            _cache.Remove(CacheKey);
        }

        public Task InvalidateCacheAsync()
        {
            _cache.Remove(CacheKey);
            return Task.CompletedTask;
        }
    }

    /// <summary>زمینه‌ی اجرای سایت (دسترسی سریع به تنظیمات)</summary>
    public class SiteContext : ISiteContext
    {
        private readonly ISiteSettingService _settings;
        public SiteContext(ISiteSettingService settings) => _settings = settings;

        public async Task<string> GetSettingAsync(string key, CancellationToken ct = default) =>
            await _settings.GetAsync(key, ct);
    }
}
