using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace UnoWebTemplate.Server.Hubs
{
    public class StatusHub : Hub
    {
        private readonly ILogger<StatusHub> _logger;

        public StatusHub(ILogger<StatusHub> logger)
        {
            _logger = logger;
        }

        public async Task<object> GetStatus()
        {
            _logger.LogInformation("SignalR Status endpoint queried.");
            return new
            {
                Status = "Healthy",
                Timestamp = DateTimeOffset.UtcNow,
                Message = "Welcome to the UnoWebTemplate API (via SignalR)"
            };
        }
    }
}
