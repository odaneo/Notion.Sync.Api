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
        public static IServiceCollection AddAppDbContext(this IServiceCollection services, string connectionString)
        {
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
            services.AddScoped<INotionArticleRepository, NotionArticleRepository>();
            services.AddScoped<IArticleRepository, ArticleRepository>();

            services.AddScoped<ITagService, TagService>();
            services.AddScoped<INotionArticleService, NotionArticleService>();
            services.AddScoped<IArticleService, ArticleService>();

            return services;
        }
    }
}
