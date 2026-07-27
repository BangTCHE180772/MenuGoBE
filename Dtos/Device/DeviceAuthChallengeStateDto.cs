using System;

namespace MenuGoBE.Dtos.Device
{
    public class DeviceAuthChallengeStateDto
    {
        public string ChallengeId { get; set; } = string.Empty;
        public string DeviceToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        
        public string? ScannedByEmail { get; set; }
        public string? ScannedByName { get; set; }
        public bool IsScanned { get; set; } = false;
    }
}
