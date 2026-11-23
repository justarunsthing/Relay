using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Relay.Api.Tests;

public sealed class RelayApiFactory : WebApplicationFactory<Program>
{
    public RelayApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", TestConnectionString());
        Environment.SetEnvironmentVariable("Jwt__Key", "relay-tests-jwt-key-must-be-32-bytes!");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = TestConnectionString(),
                ["Jwt:Key"] = "relay-tests-jwt-key-must-be-32-bytes!"
            });
        });
    }

    private static string TestConnectionString()
    {
        var secrets = new ConfigurationBuilder()
            .AddUserSecrets("bf42ffd6-6e8e-43c9-8c78-b34577db2df4")
            .Build();

        var current = secrets.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(current))
        {
            throw new InvalidOperationException(
                "Set ConnectionStrings:DefaultConnection in user secrets before running the API tests.");
        }

        return new NpgsqlConnectionStringBuilder(current)
        {
            Database = "RelayTests"
        }.ConnectionString;
    }
}
