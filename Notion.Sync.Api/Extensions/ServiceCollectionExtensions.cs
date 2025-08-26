using Notion.Sync.Api.Business.IServices;
using Notion.Sync.Api.Business.Services;
using Notion.Sync.Api.Repository.IRepositories;
using Notion.Sync.Api.Repository.Repositories;
using Npgsql;

namespace Notion.Sync.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static string BuildFinalConnString(this IConfiguration configuration, bool isDev)
        {
            var template = configuration["DbContext:ConnectionString"]
                ?? throw new InvalidOperationException("no DbContext:ConnectionString");

            var csb = new NpgsqlConnectionStringBuilder(template);

            if (!isDev)
            {
                csb.Username = configuration["Db:username"] ?? throw new InvalidOperationException("no username");
                csb.Password = configuration["Db:password"] ?? throw new InvalidOperationException("no password");
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
