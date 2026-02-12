using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Commonword.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    private static IReadOnlyList<Assembly> _moduleAssemblies = Array.Empty<Assembly>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public static void ConfigureModuleAssemblies(IEnumerable<Assembly> assemblies)
    {
        _moduleAssemblies = assemblies
            .Where(static assembly => assembly is not null)
            .Distinct()
            .ToArray();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var assembly in _moduleAssemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }
    }
}
