using Amazon.SecretsManager;
using Microsoft.EntityFrameworkCore;
using Notion.Sync.Api.Database;
using Notion.Sync.Api.Extensions;
using Serilog;
using W4k.Extensions.Configuration.Aws.SecretsManager;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

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
    builder.Configuration.AddEnvironmentVariables().AddCommandLine(args);
}

var schema = builder.Configuration.GetDatabaseSchema();
if (builder.Environment.IsDevelopment() && schema != "blog_test")
    throw new InvalidOperationException("Development must use blog_test.");
var connectionString = builder.Configuration.BuildFinalConnString();
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString,
    postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", schema)));
builder.Services.AddServices();

if (!builder.Environment.IsDevelopment())
    builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(7031));

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthorization();
app.MapControllers();
app.Run();
