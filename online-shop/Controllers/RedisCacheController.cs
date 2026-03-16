using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace ECommerce.Controller;

[ApiController]
[Route("api/redis/cache")]
public sealed class RedisCacheController : ControllerBase
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheController> _logger;

    public RedisCacheController(
        IConnectionMultiplexer connectionMultiplexer,
        IDatabase database,
        ILogger<RedisCacheController> logger)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _database = database;
        _logger = logger;
    }

    [HttpGet("{key}")]
    public async Task<ActionResult<RedisStringValueResponse>> Get(
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest("Redis key is required.");
        }

        var value = await _database.StringGetAsync(key);
        if (!value.HasValue)
        {
            _logger.LogWarning("Redis key not found. Key: {Key}.", key);
            return NotFound($"Redis key '{key}' was not found.");
        }

        var ttl = await _database.KeyTimeToLiveAsync(key);

        _logger.LogInformation("Redis key read. Key: {Key}, HasTtl: {HasTtl}.", key, ttl.HasValue);

        return Ok(new RedisStringValueResponse(
            key,
            value.ToString(),
            ttl?.TotalSeconds));
    }

    [HttpPost]
    public async Task<ActionResult<RedisMutationResponse>> Set(
        [FromBody] RedisSetKeyRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.Key))
        {
            return BadRequest("Redis key is required.");
        }

        var expiry = NormalizeExpirySeconds(request.TtlSeconds);
        var created = await _database.StringSetAsync(request.Key.Trim(), request.Value ?? string.Empty, expiry);

        _logger.LogInformation(
            "Redis key written. Key: {Key}, TtlSeconds: {TtlSeconds}, Created: {Created}.",
            request.Key,
            expiry?.TotalSeconds,
            created);

        return Ok(new RedisMutationResponse(request.Key.Trim(), created, "set"));
    }

    [HttpDelete("{key}")]
    public async Task<ActionResult<RedisMutationResponse>> Delete(
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest("Redis key is required.");
        }

        var deleted = await _database.KeyDeleteAsync(key);

        _logger.LogInformation("Redis key delete requested. Key: {Key}, Deleted: {Deleted}.", key, deleted);
        return Ok(new RedisMutationResponse(key, deleted, "delete"));
    }

    [HttpGet("exists/{key}")]
    public async Task<ActionResult<RedisExistsResponse>> Exists(
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest("Redis key is required.");
        }

        var exists = await _database.KeyExistsAsync(key);

        _logger.LogInformation("Redis key exists check. Key: {Key}, Exists: {Exists}.", key, exists);
        return Ok(new RedisExistsResponse(key, exists));
    }

    [HttpPut("{key}/expire")]
    public async Task<ActionResult<RedisExpireResponse>> Expire(
        string key,
        [FromQuery] long ttlSeconds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest("Redis key is required.");
        }

        if (ttlSeconds <= 0)
        {
            return BadRequest("ttlSeconds must be greater than zero.");
        }

        var applied = await _database.KeyExpireAsync(key, TimeSpan.FromSeconds(ttlSeconds));
        var ttl = await _database.KeyTimeToLiveAsync(key);

        _logger.LogInformation(
            "Redis expire requested. Key: {Key}, RequestedTtl: {RequestedTtl}, Applied: {Applied}, ActualTtl: {ActualTtl}.",
            key,
            ttlSeconds,
            applied,
            ttl?.TotalSeconds);

        return Ok(new RedisExpireResponse(key, applied, ttl?.TotalSeconds));
    }

    [HttpPost("increment/{key}")]
    public async Task<ActionResult<RedisIncrementResponse>> Increment(
        string key,
        [FromQuery] long by = 1,
        [FromQuery] long? ttlSeconds = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest("Redis key is required.");
        }

        var current = await _database.StringIncrementAsync(key, by);

        // TTL ставим только если он явно передан.
        if (ttlSeconds.HasValue && ttlSeconds.Value > 0)
        {
            await _database.KeyExpireAsync(key, TimeSpan.FromSeconds(ttlSeconds.Value));
        }

        var ttl = await _database.KeyTimeToLiveAsync(key);

        _logger.LogInformation(
            "Redis increment completed. Key: {Key}, IncrementBy: {IncrementBy}, NewValue: {NewValue}, Ttl: {Ttl}.",
            key,
            by,
            current,
            ttl?.TotalSeconds);

        return Ok(new RedisIncrementResponse(key, by, current, ttl?.TotalSeconds));
    }

    [HttpGet("keys")]
    public ActionResult<IReadOnlyList<string>> ListKeys(
        [FromQuery] string? prefix,
        [FromQuery] int take = 100)
    {
        take = Math.Clamp(take, 1, 500);

        if (!TryGetServer(out var server, out var error))
        {
            _logger.LogWarning("Redis keys listing failed. Reason: {Error}.", error);
            return BadRequest(error);
        }

        // Команда KEYS может быть тяжелой: ограничиваем результат и используем prefix pattern.
        var pattern = string.IsNullOrWhiteSpace(prefix) ? "*" : $"{prefix}*";
        var keys = server
            .Keys(database: _database.Database, pattern: pattern, pageSize: Math.Min(200, take))
            .Take(take)
            .Select(x => x.ToString())
            .ToList();

        _logger.LogInformation(
            "Redis keys listed. Prefix: {Prefix}, Take: {Take}, ResultCount: {ResultCount}.",
            prefix,
            take,
            keys.Count);

        return Ok(keys);
    }

    [HttpDelete("by-prefix")]
    public async Task<ActionResult<RedisDeleteByPrefixResponse>> DeleteByPrefix(
        [FromQuery] string prefix,
        [FromQuery] int take = 1000,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(prefix))
        {
            return BadRequest("prefix is required.");
        }

        take = Math.Clamp(take, 1, 5000);

        if (!TryGetServer(out var server, out var error))
        {
            _logger.LogWarning("Redis delete-by-prefix failed. Reason: {Error}.", error);
            return BadRequest(error);
        }

        var keys = server
            .Keys(database: _database.Database, pattern: $"{prefix}*", pageSize: Math.Min(500, take))
            .Take(take)
            .Select(x => (RedisKey)x.ToString())
            .ToArray();

        var deleted = keys.Length == 0 ? 0 : await _database.KeyDeleteAsync(keys);

        _logger.LogInformation(
            "Redis keys deleted by prefix. Prefix: {Prefix}, Scanned: {Scanned}, Deleted: {Deleted}.",
            prefix,
            keys.Length,
            deleted);

        return Ok(new RedisDeleteByPrefixResponse(prefix, keys.Length, deleted));
    }

    private static TimeSpan? NormalizeExpirySeconds(long? ttlSeconds)
    {
        if (!ttlSeconds.HasValue || ttlSeconds.Value <= 0)
        {
            return null;
        }

        var normalized = Math.Clamp(ttlSeconds.Value, 1, 60L * 60 * 24 * 365);
        return TimeSpan.FromSeconds(normalized);
    }

    private bool TryGetServer(out IServer server, out string? error)
    {
        var endpoints = _connectionMultiplexer.GetEndPoints();
        if (endpoints.Length == 0)
        {
            server = null!;
            error = "Redis endpoint was not found.";
            return false;
        }

        server = _connectionMultiplexer.GetServer(endpoints[0]);
        if (server is null || !server.IsConnected)
        {
            error = "Redis server is not connected.";
            return false;
        }

        error = null;
        return true;
    }
}

public sealed record RedisSetKeyRequest(string Key, string? Value, long? TtlSeconds);

public sealed record RedisMutationResponse(string Key, bool Success, string Operation);

public sealed record RedisExistsResponse(string Key, bool Exists);

public sealed record RedisStringValueResponse(string Key, string Value, double? TtlSeconds);

public sealed record RedisExpireResponse(string Key, bool Applied, double? TtlSeconds);

public sealed record RedisIncrementResponse(string Key, long IncrementBy, long NewValue, double? TtlSeconds);

public sealed record RedisDeleteByPrefixResponse(string Prefix, int ScannedKeys, long DeletedKeys);
