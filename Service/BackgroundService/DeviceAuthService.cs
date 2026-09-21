using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MenuGoBE.Dtos.Device;
using MenuGoBE.Interface.Services;

namespace MenuGoBE.Service
{
    public class DeviceAuthService : Microsoft.Extensions.Hosting.BackgroundService, IDeviceAuthService
    {
        private readonly ConcurrentDictionary<string, DeviceAuthChallengeStateDto> _challenges = new();
        private readonly ILogger<DeviceAuthService> _logger;

        public DeviceAuthService(ILogger<DeviceAuthService> logger)
        {
            _logger = logger;
        }

        public DeviceAuthChallengeStateDto CreateChallenge(string deviceToken)
        {
            var challengeId = Guid.NewGuid().ToString();
            var challenge = new DeviceAuthChallengeStateDto
            {
                ChallengeId = challengeId,
                DeviceToken = deviceToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5) 
            };

            _challenges.TryAdd(challengeId, challenge);
            return challenge;
        }

        public DeviceAuthChallengeStateDto? GetChallenge(string challengeId)
        {
            if (_challenges.TryGetValue(challengeId, out var challenge))
            {
                if (challenge.ExpiresAt > DateTime.UtcNow)
                {
                    return challenge;
                }
                _challenges.TryRemove(challengeId, out _);
            }
            return null;
        }

        public bool MarkAsScanned(string challengeId, string email, string name)
        {
            var challenge = GetChallenge(challengeId);
            if (challenge != null && !challenge.IsScanned)
            {
                challenge.IsScanned = true;
                challenge.ScannedByEmail = email;
                challenge.ScannedByName = name;
                return true;
            }
            return false;
        }

        public void RemoveChallenge(string challengeId)
        {
            _challenges.TryRemove(challengeId, out _);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                foreach (var kvp in _challenges)
                {
                    if (kvp.Value.ExpiresAt <= now)
                    {
                        _challenges.TryRemove(kvp.Key, out _);
                    }
                }
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
