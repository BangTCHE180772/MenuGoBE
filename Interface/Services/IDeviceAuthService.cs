using System;
using MenuGoBE.Dtos.Device;

namespace MenuGoBE.Interface.Services
{
    public interface IDeviceAuthService
    {
        DeviceAuthChallengeStateDto CreateChallenge(string deviceToken);
        DeviceAuthChallengeStateDto? GetChallenge(string challengeId);
        bool MarkAsScanned(string challengeId, string email, string name);
        void RemoveChallenge(string challengeId);
    }
}
