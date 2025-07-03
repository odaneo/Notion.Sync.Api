using Microsoft.EntityFrameworkCore;
using Notion.Sync.Api.Database;

namespace Notion.Sync.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAppDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            string connectionString = configuration["DbContext:ConnectionString"] ??
              throw new InvalidOperationException("Database connection string not found!");

            services.AddDbContext<AppDbContext>(option =>
            {
                option.UseNpgsql(connectionString);
            });

            return services;
        }
    }
}
