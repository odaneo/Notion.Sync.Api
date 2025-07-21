using Hangfire.Dashboard;

namespace Notion.Sync.Api.Extensions
{
    public class AllowAllDashboardAuthorization : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context) => true;
    }
}
