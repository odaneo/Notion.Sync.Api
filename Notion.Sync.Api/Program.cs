using Amazon.SecretsManager;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Notion.Sync.Api.Database;
using Notion.Sync.Api.Extensions;
using Notion.Sync.Api.Job;
using Serilog;
using W4k.Extensions.Configuration.Aws.SecretsManager;

var builder = WebApplication.CreateBuilder(args);

var isDev = builder.Environment.IsDevelopment();

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "NotionApi")
    .CreateLogger();
builder.Host.UseSerilog();

// AWS
if (!isDev)
{
    builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions("AWS"));
    builder.Services.AddAWSService<IAmazonSecretsManager>();
    builder.Configuration
        .AddSecretsManager(
            builder.Configuration["AWS:SecretNameNotionToken"]!,
            configurationKeyPrefix: "NotionToken"
        )
        .AddSecretsManager(
            builder.Configuration["AWS:SecretNameHangfireUser"]!,
            configurationKeyPrefix: "HangfireUser"
        )
        .AddSecretsManager(
            builder.Configuration["AWS:SecretNameSupabase"]!,
            configurationKeyPrefix: "Supabase"
        );
}

//DB
var finalConnStr = builder.Configuration.BuildFinalConnString(isDev);
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(finalConnStr));

// Hangfire
if (isDev)
{
    builder.Services.AddHangfire(x => x.UseMemoryStorage());
}
else
{
    builder.Services.AddHangfire((sp, cfg) =>
    {
        cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
           .UseSimpleAssemblyNameTypeSerializer()
           .UseRecommendedSerializerSettings()
           .UsePostgreSqlStorage(opts =>
           {
               opts.UseNpgsqlConnection(finalConnStr);
           });
    });
}
builder.Services.AddHangfireServer();

builder.Services.AddTransient<NotionDatabaseSyncJobService>();

builder.Services.AddServices();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

if (!isDev)
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(7031);
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}
else
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new BasicAuthDashboardAuthorization(app.Configuration)]
    });
}

RecurringJob.AddOrUpdate<NotionDatabaseSyncJobService>(
    "SyncTagsAndArticleListAsync",
    job => job.SyncTagsAndArticleListAsync(),
    "0 */2 * * *",
    new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time")
    }
);

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
