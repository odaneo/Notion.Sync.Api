using Hangfire.Dashboard;
using Hangfire.Dashboard.BasicAuthorization;

namespace Notion.Sync.Api.Extensions
{
    public class BasicAuthDashboardAuthorization : IDashboardAuthorizationFilter
    {
        private readonly IDashboardAuthorizationFilter _inner;
        public BasicAuthDashboardAuthorization(IConfiguration configuration)
        {
            var user = configuration["HangfireUser:user"];
            var password = configuration["HangfireUser:password"];
            var authUser = new BasicAuthAuthorizationUser { Login = user, PasswordClear = password };

            _inner = new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
            {
                RequireSsl = true,
                SslRedirect = true,
                LoginCaseSensitive = true,
                Users = [authUser]
            });
        }
        public bool Authorize(DashboardContext context) => _inner.Authorize(context);
    }
}
