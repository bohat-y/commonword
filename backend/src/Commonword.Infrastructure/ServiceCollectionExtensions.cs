using System.Reflection;
using Commonword.Infrastructure.Persistence;
using Commonword.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Commonword.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] moduleAssemblies)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? configuration["ConnectionStrings__Default"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'Default' was not configured.");
        }

        AppDbContext.ConfigureModuleAssemblies(moduleAssemblies);

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            });
        });

        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
