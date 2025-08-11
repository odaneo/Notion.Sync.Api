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
        public static IServiceCollection AddAppDbContext(this IServiceCollection services, bool isDevelopment, IConfiguration configuration)
        {
            var template = configuration["DbContext:ConnectionString"] ??
                throw new InvalidOperationException("Database connection string not found!");

            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                var conn = template;
                if (!isDevelopment)
                {
                    var user = configuration["Db:username"]
                               ?? throw new InvalidOperationException("Db:username 未加载");
                    var pass = configuration["Db:password"]
                               ?? throw new InvalidOperationException("Db:password 未加载");
                    conn = conn
                        .Replace("{USERNAME}", user)
                        .Replace("{PASSWORD}", pass);
                }
                options.UseNpgsql(conn);
            });

            return services;
        }
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddScoped<ITagRepository, TagRepository>();
            services.AddScoped<ISubTagRepository, SubTagRepository>();
            services.AddScoped<INotionArticleRepository, NotionArticleRepository>();
            services.AddScoped<IArticleRepository, ArticleRepository>();

            services.AddScoped<ITagService, TagService>();
            services.AddScoped<INotionArticleService, NotionArticleService>();
            services.AddScoped<IArticleService, ArticleService>();

            return services;
        }
    }
}
