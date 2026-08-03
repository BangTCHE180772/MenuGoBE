using MenuGoBE.Interface.Services;
using StackExchange.Redis;

namespace MenuGoBE.Service
{
    public class RedisInventoryService : IRedisInventoryService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;

        public RedisInventoryService(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _db = _redis.GetDatabase();
        }

        private const string DirtyKeysSet = "inventory:dirty_keys";

        private string GetKey(int branchId, int productId)
        {
            return $"inventory:reserved:branch:{branchId}:product:{productId}";
        }

        public async Task<long> IncrementReservedQuantityAsync(int branchId, int productId, int quantity)
        {
            var result = await _db.StringIncrementAsync(GetKey(branchId, productId), quantity);
            await _db.SetAddAsync(DirtyKeysSet, $"{branchId}:{productId}");
            return result;
        }

        public async Task<long> DecrementReservedQuantityAsync(int branchId, int productId, int quantity)
        {
            var result = await _db.StringDecrementAsync(GetKey(branchId, productId), quantity);
            await _db.SetAddAsync(DirtyKeysSet, $"{branchId}:{productId}");
            return result;
        }

        public async Task<long> GetReservedQuantityAsync(int branchId, int productId)
        {
            var value = await _db.StringGetAsync(GetKey(branchId, productId));
            return value.HasValue ? (long)value : 0;
        }

        public async Task<Dictionary<int, long>> GetReservedQuantitiesAsync(int branchId, IEnumerable<int> productIds)
        {
            var keys = productIds.Select(p => (RedisKey)GetKey(branchId, p)).ToArray();
            var values = await _db.StringGetAsync(keys);

            var result = new Dictionary<int, long>();
            int i = 0;
            foreach (var productId in productIds)
            {
                result[productId] = values[i].HasValue ? (long)values[i] : 0;
                i++;
            }
            return result;
        }

        public async Task SetReservedQuantityAsync(int branchId, int productId, long quantity)
        {
            await _db.StringSetAsync(GetKey(branchId, productId), quantity);
            await _db.SetAddAsync(DirtyKeysSet, $"{branchId}:{productId}");
        }

        public async Task<Dictionary<string, long>> GetAllReservedQuantitiesAsync()
        {
            var result = new Dictionary<string, long>();
            var endpoints = _redis.GetEndPoints();
            var server = _redis.GetServer(endpoints.First());

            // Scan all keys matching the pattern
            var keys = server.Keys(pattern: "inventory:reserved:branch:*:product:*").ToArray();
            
            if (keys.Length == 0) return result;

            var values = await _db.StringGetAsync(keys);

            for (int i = 0; i < keys.Length; i++)
            {
                if (values[i].HasValue)
                {
                    // return dictionary mapping "branchId:productId" -> quantity
                    // Key format: inventory:reserved:branch:{branchId}:product:{productId}
                    var keyStr = keys[i].ToString();
                    var parts = keyStr.Split(':');
                    if (parts.Length == 6)
                    {
                        var branchId = parts[3];
                        var productId = parts[5];
                        result[$"{branchId}:{productId}"] = (long)values[i];
                    }
                }
            }

            return result;
        }

        public async Task<Dictionary<string, long>> GetAndClearDirtyReservedQuantitiesAsync()
        {
            var result = new Dictionary<string, long>();

            var script = @"
                local members = redis.call('SMEMBERS', KEYS[1])
                if #members > 0 then
                    redis.call('DEL', KEYS[1])
                end
                return members
            ";

            var scriptResult = await _db.ScriptEvaluateAsync(script, new RedisKey[] { DirtyKeysSet });
            var dirtyKeysStr = (string[]?)scriptResult;

            if (dirtyKeysStr == null || dirtyKeysStr.Length == 0)
                return result;

            var redisKeys = dirtyKeysStr.Select(k => {
                var parts = k.Split(':');
                return (RedisKey)GetKey(int.Parse(parts[0]), int.Parse(parts[1]));
            }).ToArray();

            var values = await _db.StringGetAsync(redisKeys);

            for (int i = 0; i < dirtyKeysStr.Length; i++)
            {
                if (values[i].HasValue)
                {
                    result[dirtyKeysStr[i]] = (long)values[i];
                }
            }

            return result;
        }
    }
}
