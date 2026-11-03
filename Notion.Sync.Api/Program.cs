using Amazon.SecretsManager;
using Microsoft.EntityFrameworkCore;
using Notion.Sync.Api.Database;
using Notion.Sync.Api.Extensions;
using Notion.Sync.Api.Sync;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using W4k.Extensions.Configuration.Aws.SecretsManager;

if (args.Contains("--migrate"))
{
    await using var db = new AppDbContextFactory().CreateDbContext(args.Where(arg => arg != "--migrate").ToArray());
    await db.Database.MigrateAsync();
    Console.WriteLine("Database migrations completed.");
    return;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "NotionApi")
    .CreateLogger();
builder.Host.UseSerilog();

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions("AWS"));
    builder.Services.AddAWSService<IAmazonSecretsManager>();
    builder.Configuration.AddSecretsManager(
        builder.Configuration["AWS:SecretNameSupabase"]!, configurationKeyPrefix: "Supabase");
    builder.Configuration.AddSecretsManager(
        builder.Configuration["AWS:SecretNameSync"]!, configurationKeyPrefix: "Sync");
    builder.Configuration.AddSecretsManager(
        builder.Configuration["AWS:SecretNameRevalidation"]!, configurationKeyPrefix: "Revalidation");
    builder.Configuration.AddEnvironmentVariables().AddCommandLine(args);
}

var schema = builder.Configuration.GetDatabaseSchema();
if (builder.Environment.IsDevelopment() && schema != "blog_test")
    throw new InvalidOperationException("Development must use blog_test.");
var connectionString = builder.Configuration.BuildFinalConnString();
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString,
    postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", schema)));
builder.Services.AddOptions<SyncOptions>().Bind(builder.Configuration.GetSection("Sync"))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Repository) && !string.IsNullOrWhiteSpace(options.Branch), "Sync repository and branch are required.")
    .Validate(options => options.Token is { Length: >= 32 and <= 512 }, "Sync:Token must contain at least 32 characters.").ValidateOnStart();
builder.Services.AddOptions<RevalidationOptions>().Bind(builder.Configuration.GetSection("Revalidation"))
    .Validate(options => options.Token is { Length: >= 32 and <= 512 }, "Revalidation:Token must contain at least 32 characters.")
    .Validate(options => Uri.TryCreate(options.Url, UriKind.Absolute, out var url) &&
        (builder.Environment.IsDevelopment()
            ? (url.IsLoopback || url.Host == "host.docker.internal") && url.Scheme is "http" or "https"
            : url.Scheme == "https" && !url.IsLoopback),
        "Revalidation:Url must be local in Development and HTTPS in Production.").ValidateOnStart();
builder.Services.AddScoped<SyncTokenFilter>();
builder.Services.AddScoped<ObsidianSyncService>();
builder.Services.AddHttpClient<RevalidationClient>(client => client.Timeout = TimeSpan.FromSeconds(20))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddFixedWindowLimiter("sync", limiter =>
    {
        limiter.PermitLimit = 30;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
});

if (!builder.Environment.IsDevelopment())
    builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(7031));

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.Run();

public partial class Program { }
