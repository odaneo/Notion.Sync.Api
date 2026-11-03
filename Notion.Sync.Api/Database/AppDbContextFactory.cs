using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Notion.Sync.Api.Extensions;
using W4k.Extensions.Configuration.Aws.SecretsManager;

namespace Notion.Sync.Api.Database;

// EF uses migration credentials without starting the web server or old synchronization jobs.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var arguments = new ConfigurationBuilder().AddCommandLine(args).Build();
        var environment = arguments["environment"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        if (environment is not ("Development" or "Production"))
            throw new InvalidOperationException("Use Development or Production for database migrations.");
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environment}.json");
        if (environment == "Development")
            builder.AddUserSecrets<AppDbContext>();
        else
        {
            var settings = builder.AddEnvironmentVariables().Build();
            builder.AddSecretsManager(settings["AWS:SecretNameSupabaseMigration"]
                ?? throw new InvalidOperationException("AWS:SecretNameSupabaseMigration is not configured."),
                configurationKeyPrefix: "SupabaseMigration");
        }
        var configuration = builder.AddEnvironmentVariables().AddCommandLine(args).Build();
        var schema = configuration.GetDatabaseSchema();
        if (environment == "Development" && schema != "blog_test")
            throw new InvalidOperationException("Development migrations must target blog_test.");
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(configuration.BuildFinalConnString(migration: true), postgres =>
                postgres.MigrationsHistoryTable("__EFMigrationsHistory", schema))
            .Options);
    }
}
