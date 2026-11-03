using Notion.Sync.Api.Repository.IRepositories;
using Notion.Sync.Api.Repository.Repositories;
using Npgsql;

namespace Notion.Sync.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static string GetDatabaseSchema(this IConfiguration configuration)
    {
        var schema = configuration["Database:Schema"]
            ?? throw new InvalidOperationException("Database:Schema is not configured.");
        return schema is "blog" or "blog_test" ? schema
            : throw new InvalidOperationException("Database:Schema must be blog or blog_test.");
    }

    public static string BuildFinalConnString(this IConfiguration configuration, bool migration = false)
    {
        var key = migration ? "DbContext:MigrationConnectionString" : "DbContext:ConnectionString";
        var credentials = migration ? "SupabaseMigration" : "Supabase";
        var template = configuration[key]
            ?? throw new InvalidOperationException($"{key} is not configured.");
        var builder = new NpgsqlConnectionStringBuilder(template)
        {
            Username = configuration[$"{credentials}:username"]
                ?? throw new InvalidOperationException($"{credentials}:username is not configured."),
            Password = configuration[$"{credentials}:password"]
                ?? throw new InvalidOperationException($"{credentials}:password is not configured."),
            SearchPath = configuration.GetDatabaseSchema()
        };
        if (configuration["Database:RootCertificate"] is { Length: > 0 } certificate)
            builder.RootCertificate = Path.GetFullPath(certificate, AppContext.BaseDirectory);
        if (string.IsNullOrWhiteSpace(builder.Username) || string.IsNullOrWhiteSpace(builder.Password))
            throw new InvalidOperationException($"{credentials} credentials must not be empty.");
        var writerRole = builder.SearchPath == "blog_test" ? "blog_test_writer" : "blog_writer";
        if (!migration && !builder.Username.StartsWith(writerRole + ".", StringComparison.Ordinal))
            throw new InvalidOperationException($"The runtime connection must use {writerRole}, not an administrator.");
        if (builder.SslMode != SslMode.VerifyFull)
            throw new InvalidOperationException($"{key} must use SSL Mode=VerifyFull.");
        return builder.ConnectionString;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ISubTagRepository, SubTagRepository>();
        services.AddScoped<IArticleRepository, ArticleRepository>();
        return services;
    }
}
