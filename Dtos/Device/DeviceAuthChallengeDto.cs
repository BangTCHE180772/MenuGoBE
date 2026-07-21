using System;

namespace MenuGoBE.Dtos.Device
{
    public class DeviceAuthChallengeDto
    {
        public string ChallengeId { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
