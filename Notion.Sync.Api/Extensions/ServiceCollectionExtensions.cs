using Microsoft.EntityFrameworkCore;
using Notion.Sync.Api.Business.IServices;
using Notion.Sync.Api.Business.Services;
using Notion.Sync.Api.Database;
using Notion.Sync.Api.Repository.IRepositories;
using Notion.Sync.Api.Repository.Repositories;

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
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddScoped<ITagRepository, TagRepository>();
            services.AddScoped<ISubTagRepository, SubTagRepository>();
            services.AddScoped<ITagService, TagService>();

            return services;
        }
    }
}
