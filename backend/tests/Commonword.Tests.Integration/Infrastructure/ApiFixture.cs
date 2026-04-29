using Commonword.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Commonword.Tests.Integration.Infrastructure;

[CollectionDefinition("Api")]
public class ApiCollection : ICollectionFixture<ApiFixture> { }

public class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16")
        .Build();

    private WebApplicationFactory<Program> _factory = default!;

    public HttpClient Client { get; private set; } = default!;
    public string ConnectionString { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:Default", ConnectionString);
                builder.UseSetting("Admin:Key", "test-admin-key");
            });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        Client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
        await _container.DisposeAsync();
    }

    public async Task ResetAsync(params string[] tables)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        foreach (var table in tables)
        {
            await using var cmd = new NpgsqlCommand($"TRUNCATE TABLE {table} CASCADE", conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
