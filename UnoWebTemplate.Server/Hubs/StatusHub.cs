using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using UnoWebTemplate.Shared.Models;

namespace UnoWebTemplate.Server.Hubs
{
    public class StatusHub : Hub
    {
        private readonly ILogger<StatusHub> _logger;

        public StatusHub(ILogger<StatusHub> logger)
        {
            _logger = logger;
        }

        public async Task<StatusResponse> GetStatus()
        {
            _logger.LogInformation("SignalR Status endpoint queried.");
            return new StatusResponse
            {
                Status = "Healthy",
                Timestamp = DateTimeOffset.UtcNow,
                Message = "Welcome to the UnoWebTemplate API (via SignalR)"
            };
        }
    }
}
