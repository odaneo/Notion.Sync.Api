using Notion.Sync.Api.Business.IServices;
using Notion.Sync.Api.Business.Services;
using Notion.Sync.Api.Repository.IRepositories;
using Notion.Sync.Api.Repository.Repositories;
using Npgsql;

namespace Notion.Sync.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static string BuildFinalConnString(this IConfiguration cfg, bool isDev)
        {
            var template = cfg["DbContext:ConnectionString"]
                ?? throw new InvalidOperationException("DbContext:ConnectionString 未配置");

            var csb = new NpgsqlConnectionStringBuilder(template);

            if (!isDev)
            {
                csb.Username = cfg["Db:username"] ?? throw new InvalidOperationException("Db:username 未加载");
                csb.Password = cfg["Db:password"] ?? throw new InvalidOperationException("Db:password 未加载");
            }
            return csb.ConnectionString;
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
