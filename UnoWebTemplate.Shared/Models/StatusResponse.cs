using System;

namespace UnoWebTemplate.Shared.Models
{
    public class StatusResponse
    {
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
