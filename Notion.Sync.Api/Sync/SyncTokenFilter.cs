using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Notion.Sync.Api.Sync;

public sealed class SyncTokenFilter(IOptions<SyncOptions> options) : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var header = context.HttpContext.Request.Headers.Authorization;
        if (header.Count != 1 || !AuthenticationHeaderValue.TryParse(header[0], out var authorization) ||
            !authorization.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase) || authorization.Parameter is not { Length: > 0 and <= 512 } token ||
            !CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(token)),
                SHA256.HashData(Encoding.UTF8.GetBytes(options.Value.Token))))
            context.Result = new UnauthorizedResult();
    }
}
