using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soransoft.Application.Interfaces;
using Soransoft.Infrastructure.Imaging;
using Soransoft.Infrastructure.Identity;
using Soransoft.Infrastructure.Persistence;
using Soransoft.Infrastructure.Storage;

namespace Soransoft.Infrastructure.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<SoransoftDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sql => sql.MigrationsHistoryTable("__SoransoftMigrationsHistory")));

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SoransoftDbContext>());
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<IImageOptimizer, SkiaImageOptimizer>();
            services.AddScoped<IFileStorage, LocalFileStorage>();
            services.AddScoped<ISiteSettingService, SiteSettingService>();
            services.AddScoped<ISiteFeatureService, SiteFeatureService>();
            services.AddScoped<ISiteContext, SiteContext>();
            services.AddScoped<ISiteQueries, SiteQueries>();
            services.AddScoped<IFormService, FormService>();
            services.AddScoped<IUserAccountService, UserAccountService>();
            services.AddScoped<IPartnerPortalService, PartnerPortalService>();
            services.AddScoped<IPartnerDocumentService, PartnerDocumentService>();
            services.AddScoped<IWalletService, WalletService>();
            services.AddScoped<ISeoService, SeoService>();
            services.AddScoped<IMediaLibraryService, LocalMediaLibraryService>();
            services.AddHttpContextAccessor();
            services.AddMemoryCache();

            return services;
        }
    }
}
