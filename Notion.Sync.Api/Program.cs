using Amazon.SecretsManager;
using Hangfire;
using Hangfire.MemoryStorage;
using Notion.Sync.Api.Extensions;
using Notion.Sync.Api.Job;
using W4k.Extensions.Configuration.Aws.SecretsManager;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Hangfire
builder.Services.AddHangfire(x => x.UseMemoryStorage());
builder.Services.AddHangfireServer();
builder.Services.AddHttpClient();
builder.Services.AddTransient<NotionDatabaseSyncJobService>();

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// AWS
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions("AWS"));
    builder.Services.AddAWSService<IAmazonSecretsManager>();
    builder.Configuration
        .AddSecretsManager(
            builder.Configuration["AWS:SecretNameRDS"]!,
            configurationKeyPrefix: "Db"
        )
        .AddSecretsManager(
            builder.Configuration["AWS:SecretNameNotionToken"]!,
            configurationKeyPrefix: "NotionToken"
        );
}
//DB
builder.Services.AddAppDbContext(builder.Environment.IsDevelopment(), builder.Configuration);

builder.Services.AddServices();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

if (!builder.Environment.IsDevelopment())
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

app.UseHangfireDashboard("/hangfirehangfirehangfirehangfirehangfire", new DashboardOptions
{
    Authorization = [new AllowAllDashboardAuthorization()]
});

RecurringJob.AddOrUpdate<NotionDatabaseSyncJobService>(
    "SyncTagsAndArticleListAsync",
    job => job.SyncTagsAndArticleListAsync(),
    "0 3 * * *");

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
