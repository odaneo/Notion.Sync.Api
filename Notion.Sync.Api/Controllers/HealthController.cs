using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Sockets;

namespace Notion.Sync.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult HealthCheck()
        {
            string localIp = GetLocalIpAddress();
            return Ok($"Healthy\nLocal IP: {localIp}\n");
        }
        private static string GetLocalIpAddress()
        {
            string localIP = "Unknown";
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork) // IPv4
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }
    }
}
