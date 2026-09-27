using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soransoft.Application.Interfaces;
using Soransoft.Application.Services;

namespace Soransoft.Application.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ISiteService, SiteService>();
            return services;
        }
    }
}
