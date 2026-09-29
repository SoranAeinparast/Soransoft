using Soransoft.Application.Interfaces;

namespace Soransoft.Infrastructure.Persistence
{
    public sealed class SiteFeatureService : ISiteFeatureService
    {
        private readonly ISiteSettingService _settings;

        public SiteFeatureService(ISiteSettingService settings) => _settings = settings;

        public async Task<bool> IsEnabledAsync(string key, CancellationToken ct = default)
        {
            var definition = SiteFeatures.All.FirstOrDefault(f => f.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (definition is null) return false;

            var value = await _settings.GetAsync(SiteFeatures.SettingKey(definition.Key), ct);
            return string.IsNullOrWhiteSpace(value) || !bool.TryParse(value, out var enabled)
                ? definition.DefaultEnabled
                : enabled;
        }

        public async Task<IReadOnlyList<SiteFeatureState>> GetAllAsync(CancellationToken ct = default)
        {
            var values = await _settings.GetAllAsync(ct);
            return SiteFeatures.All.Select(definition => new SiteFeatureState
            {
                Key = definition.Key,
                Title = definition.Title,
                Description = definition.Description,
                Group = definition.Group,
                IsEnabled = !values.TryGetValue(SiteFeatures.SettingKey(definition.Key), out var value)
                    || !bool.TryParse(value, out var enabled)
                    ? definition.DefaultEnabled
                    : enabled,
            }).ToList();
        }

        public async Task SetEnabledAsync(string key, bool enabled, CancellationToken ct = default)
        {
            var definition = SiteFeatures.All.FirstOrDefault(f => f.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (definition is null) throw new ArgumentException("قابلیت سایت یافت نشد.", nameof(key));

            await _settings.SetAsync(SiteFeatures.SettingKey(definition.Key), enabled ? "true" : "false", ct);
        }
    }
}
