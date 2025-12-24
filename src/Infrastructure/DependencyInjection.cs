using Application.Common.Interfaces;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<LiberdadeDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("SqliteConnection")));

            services.AddScoped<IDadosMercadoService, DadosMercadoService>();
            services.Configure<NtfyConfigs>(configuration.GetSection("NtfySettings"));
            services.AddHttpClient<INotificacaoService, NtfyNotificacaoService>();

            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<LiberdadeDbContext>());
            services.AddHttpClient<IFeriadosNacionaisService, FeriadosNacionaisService>();
            services.AddTransient<IAgenteFinanceiroService, OpenRouterAgentService>();

            return services;
        }
    }
}
