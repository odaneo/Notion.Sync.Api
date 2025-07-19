using Hangfire;
using Hangfire.MemoryStorage;
using Notion.Sync.Api.Extensions;
using Notion.Sync.Api.Job;

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

builder.Services.AddHttpClient();

builder.Services.AddAppDbContext(builder.Configuration);

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
app.UseHangfireDashboard();

RecurringJob.AddOrUpdate<NotionDatabaseSyncJobService>(
    "SyncTagsAndArticleListAsync",
    job => job.SyncTagsAndArticleListAsync(),
    "*/15 * * * *");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
