using Microsoft.AspNetCore.Mvc;
using System.Net.NetworkInformation;
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
            var hostGatewayIp = GetHostGatewayIp();
            return Ok($"Healthy\nLocal IP: {hostGatewayIp}\n");
        }
        private static string GetHostGatewayIp()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces()
                       .Where(n =>
                           n.OperationalStatus == OperationalStatus.Up &&
                           n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
            {
                var props = ni.GetIPProperties();
                var gw = props.GatewayAddresses
                              .FirstOrDefault(g => g?.Address.AddressFamily == AddressFamily.InterNetwork);
                if (gw?.Address is not null)
                    return gw.Address.ToString();
            }
            return "Unknown";
        }
    }
}
