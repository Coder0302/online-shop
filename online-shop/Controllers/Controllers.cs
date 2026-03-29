using ECommerce.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using Neo4j.Driver;
using project.Models.Neo4jModels;
using project.Services;
using StackExchange.Redis;

namespace ECommerce.Controller;

[Route("api/[controller]")]
[ApiController]
public sealed class ShopController : ControllerBase
{
    private const string ProductsCacheKey = "products";
    private static readonly TimeSpan ProductsCacheTtl = TimeSpan.FromMinutes(5);

    private readonly IMongoDatabase _mongoDatabase;
    private readonly IDatabase _redisDatabase;
    private readonly ECommerceDbContext _dbContext;
    private readonly IDriver _neo4jDriver;
    private readonly INeo4jService _neo4jService;
    private readonly ILogger<ShopController> _logger;

    public ShopController(
        IMongoDatabase mongoDatabase,
        IDatabase redisDatabase,
        ECommerceDbContext dbContext,
        IDriver neo4jDriver,
        INeo4jService neo4jService,
        ILogger<ShopController> logger)
    {
        _mongoDatabase = mongoDatabase;
        _redisDatabase = redisDatabase;
        _dbContext = dbContext;
        _neo4jDriver = neo4jDriver;
        _neo4jService = neo4jService;
        _logger = logger;
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(CancellationToken cancellationToken)
    {
        var cachedProducts = await _redisDatabase.StringGetAsync(ProductsCacheKey);
        if (cachedProducts.HasValue)
        {
            _logger.LogInformation("Products returned from Redis cache.");
            return Content(cachedProducts.ToString(), "application/json");
        }

        var mongoCollection = _mongoDatabase.GetCollection<BsonDocument>("products");
        var mongoProducts = await mongoCollection
            .Find(FilterDefinition<BsonDocument>.Empty)
            .ToListAsync(cancellationToken);

        if (mongoProducts.Count > 0)
        {
            var productsJson = SerializeMongoProducts(mongoProducts);
            await _redisDatabase.StringSetAsync(ProductsCacheKey, productsJson, ProductsCacheTtl);

            _logger.LogInformation(
                "Products loaded from MongoDB and cached in Redis. Count: {ProductsCount}.",
                mongoProducts.Count);

            return Content(productsJson, "application/json");
        }

        _logger.LogWarning("MongoDB collection 'products' is empty. Using PostgreSQL fallback.");
        var postgresProduct = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (postgresProduct is null)
        {
            _logger.LogWarning("Products were not found in MongoDB and PostgreSQL.");
            return NotFound("Products are not available.");
        }

        _logger.LogInformation("Returned fallback product from PostgreSQL. ProductId: {ProductId}.", postgresProduct.ProductId);
        return Ok(postgresProduct);
    }

    [HttpPost("events")]
    public async Task<ActionResult<ShopEventAcceptedResponse>> TrackEvent(
        [FromBody] ShopEventRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateEventRequest(request);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var normalizedEventType = NormalizeEventType(request.EventType);
        var occurredAtUtc = NormalizeOccurredAt(request.OccurredAtUtc);

        await AppendEventAsync(request, normalizedEventType, occurredAtUtc, cancellationToken);

        _logger.LogInformation(
            "Shop event tracked. EventType: {EventType}, UserId: {UserId}, ProductId: {ProductId}, OccurredAtUtc: {OccurredAtUtc}.",
            normalizedEventType,
            request.UserId,
            request.ProductId,
            occurredAtUtc);

        return Ok(new ShopEventAcceptedResponse(
            normalizedEventType,
            request.UserId.Trim(),
            request.ProductId.Trim(),
            occurredAtUtc));
    }

    [HttpPost("events/batch")]
    public async Task<ActionResult<ShopEventBatchResponse>> TrackEventsBatch(
        [FromBody] ShopEventBatchRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Events is null || request.Events.Count == 0)
        {
            return BadRequest("Batch must contain at least one event.");
        }

        var processed = 0;
        var failed = 0;
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var errors = new List<ShopEventBatchError>();

        for (var index = 0; index < request.Events.Count; index++)
        {
            var item = request.Events[index];
            var validationError = ValidateEventRequest(item);
            if (validationError is not null)
            {
                if (!request.ContinueOnError)
                {
                    return BadRequest($"Event at index {index} is invalid: {validationError}");
                }

                failed++;
                errors.Add(new ShopEventBatchError(index, validationError));
                continue;
            }

            var normalizedEventType = NormalizeEventType(item.EventType);
            var occurredAtUtc = NormalizeOccurredAt(item.OccurredAtUtc);

            try
            {
                await AppendEventAsync(item, normalizedEventType, occurredAtUtc, cancellationToken);
                processed++;
                counts[normalizedEventType] = counts.TryGetValue(normalizedEventType, out var current) ? current + 1 : 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to track batch event. Index: {Index}, EventType: {EventType}, UserId: {UserId}, ProductId: {ProductId}.",
                    index,
                    normalizedEventType,
                    item.UserId,
                    item.ProductId);

                if (!request.ContinueOnError)
                {
                    throw;
                }

                failed++;
                errors.Add(new ShopEventBatchError(index, ex.Message));
            }
        }

        return Ok(new ShopEventBatchResponse(processed, failed, counts, errors));
    }

    [HttpPost("neo4j/seed-test-data")]
    public async Task<IActionResult> SeedTestData(
        [FromBody] DtoSeedData seedData,
        CancellationToken cancellationToken)
    {
        await _neo4jService.SeedTestDataAsync(
            seedData.UserCount,
            seedData.ProductCount,
            seedData.StoreCount,
            seedData.ViewedProb,
            seedData.LikedProb,
            seedData.PurchasedProb,
            seedData.BoughtTogetherProb,
            seedData.VisitedProb,
            seedData.QuantityProb,
            seedData.ShownProb);

        _logger.LogInformation(
            "Neo4j test data seeded. Users: {Users}, Products: {Products}, Stores: {Stores}.",
            seedData.UserCount,
            seedData.ProductCount,
            seedData.StoreCount);

        return Ok(new { success = true });
    }

    [HttpGet("neo4j/nodes")]
    public async Task<IActionResult> GetNodesByType([FromQuery] string type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return BadRequest("Node type is required.");
        }

        object result = type.Trim().ToLowerInvariant() switch
        {
            "user" => await _neo4jService.GetNodesByTypeAsync<UserNode>("User"),
            "product" => await _neo4jService.GetNodesByTypeAsync<ProductNode>("Product"),
            "store" => await _neo4jService.GetNodesByTypeAsync<StoreNode>("Store"),
            _ => throw new ArgumentException($"Unknown node type: {type}")
        };

        return Ok(new
        {
            success = true,
            data = result
        });
    }

    [HttpGet("analytics/viewed-products")]
    public async Task<IActionResult> GetViewedProductsByUser([FromQuery] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest("UserId is required.");
        }

        var result = await _neo4jService.GetViewedProductsByUserAsync(new UserNode { Id = userId.Trim() });
        return Ok(new
        {
            success = true,
            data = result
        });
    }

    [HttpGet("analytics/users-who-liked-product")]
    public async Task<IActionResult> GetUsersWhoLikedProduct([FromQuery] string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            return BadRequest("ProductId is required.");
        }

        var result = await _neo4jService.GetUsersWhoLikedProductAsync(new ProductNode { Id = productId.Trim() });
        return Ok(new
        {
            success = true,
            data = result
        });
    }

    [HttpGet("analytics/recommendations/by-user")]
    public async Task<IActionResult> GetRecommendedProductsByUser([FromQuery] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest("UserId is required.");
        }

        var result = await _neo4jService.GetRecommendedProductsbyUserAsync(new UserNode { Id = userId.Trim() });
        return Ok(new
        {
            success = true,
            data = result
        });
    }

    [HttpGet("analytics/recommendations/by-product")]
    public async Task<IActionResult> GetRecommendedProductsByProduct([FromQuery] string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            return BadRequest("ProductId is required.");
        }

        var result = await _neo4jService.GetRecommendedProductsbyProductAsync(new ProductNode { Id = productId.Trim() });
        return Ok(new
        {
            success = true,
            data = result
        });
    }

    [HttpGet("analytics/top-products")]
    public async Task<IActionResult> GetTopProducts(
        [FromQuery] string edgeType = "VIEWED",
        [FromQuery] int limit = 10)
    {
        limit = Math.Clamp(limit, 1, 100);
        var result = await _neo4jService.GetTopProductsAsync(edgeType.Trim().ToUpperInvariant(), limit);

        return Ok(new
        {
            success = true,
            data = result
        });
    }

    [HttpGet("analytics/tag-statistics")]
    public async Task<IActionResult> GetTagStatistics()
    {
        var result = await _neo4jService.GetTagStatisticsAsync();
        return Ok(new
        {
            success = true,
            data = result
        });
    }

    [HttpGet("analytics/top-users-by-viewed-and-purchased")]
    public async Task<IActionResult> GetTopUsersByViewedAndPurchased([FromQuery] int limit = 10)
    {
        limit = Math.Clamp(limit, 1, 100);
        var result = await _neo4jService.GetTopUsersByViewedAndPurchasedAsync(limit);

        return Ok(new
        {
            success = true,
            data = result
        });
    }

    private async Task AppendEventAsync(
        ShopEventRequest request,
        string normalizedEventType,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken)
    {
        var eventDefinition = normalizedEventType switch
        {
            "shown" => new EventDefinition("SHOWN", (int)TypeEdge.SHOWN, IsProductToUser: true),
            "viewed" => new EventDefinition("VIEWED", (int)TypeEdge.VIEWED, IsProductToUser: false),
            "liked" => new EventDefinition("LIKED", (int)TypeEdge.LIKED, IsProductToUser: false),
            "purchased" => new EventDefinition("PURCHASED", (int)TypeEdge.PURCHASED, IsProductToUser: false),
            _ => throw new ArgumentOutOfRangeException(nameof(request.EventType), request.EventType, "Unsupported event type.")
        };

        var userId = request.UserId.Trim();
        var productId = request.ProductId.Trim();
        var userName = string.IsNullOrWhiteSpace(request.UserName) ? userId : request.UserName.Trim();
        var productName = string.IsNullOrWhiteSpace(request.ProductName) ? productId : request.ProductName.Trim();
        var productTags = request.ProductTags?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();

        var edgeProperties = new Dictionary<string, object>
        {
            ["name"] = eventDefinition.RelationshipType,
            ["type"] = eventDefinition.EdgeType,
            ["date"] = occurredAtUtc
        };

        if (normalizedEventType == "purchased" && request.Rating.HasValue)
        {
            edgeProperties["rating"] = Math.Clamp(request.Rating.Value, 1, 5);
        }

        var query = eventDefinition.IsProductToUser
            ? $$"""
               MERGE (u:User {ext_id: $userId})
               ON CREATE SET u.name = $userName, u.type = $userType
               ON MATCH SET u.name = CASE WHEN $userName = '' THEN u.name ELSE $userName END
               MERGE (p:Product {ext_id: $productId})
               ON CREATE SET p.name = $productName, p.type = $productType, p.tags = $productTags, p.createdAt = $productCreatedAt
               ON MATCH SET
                   p.name = CASE WHEN $productName = '' THEN p.name ELSE $productName END,
                   p.tags = CASE WHEN size($productTags) = 0 THEN coalesce(p.tags, []) ELSE $productTags END,
                   p.createdAt = coalesce(p.createdAt, $productCreatedAt)
               CREATE (p)-[r:{{eventDefinition.RelationshipType}}]->(u)
               SET r += $edgeProperties
               RETURN elementId(r) as relationshipId
               """
            : $$"""
               MERGE (u:User {ext_id: $userId})
               ON CREATE SET u.name = $userName, u.type = $userType
               ON MATCH SET u.name = CASE WHEN $userName = '' THEN u.name ELSE $userName END
               MERGE (p:Product {ext_id: $productId})
               ON CREATE SET p.name = $productName, p.type = $productType, p.tags = $productTags, p.createdAt = $productCreatedAt
               ON MATCH SET
                   p.name = CASE WHEN $productName = '' THEN p.name ELSE $productName END,
                   p.tags = CASE WHEN size($productTags) = 0 THEN coalesce(p.tags, []) ELSE $productTags END,
                   p.createdAt = coalesce(p.createdAt, $productCreatedAt)
               CREATE (u)-[r:{{eventDefinition.RelationshipType}}]->(p)
               SET r += $edgeProperties
               RETURN elementId(r) as relationshipId
               """;

        var parameters = new Dictionary<string, object>
        {
            ["userId"] = userId,
            ["userName"] = userName,
            ["userType"] = (int)TypeNode.USER,
            ["productId"] = productId,
            ["productName"] = productName,
            ["productType"] = (int)TypeNode.PRODUCT,
            ["productTags"] = productTags,
            ["productCreatedAt"] = occurredAtUtc,
            ["edgeProperties"] = edgeProperties
        };

        await using var session = _neo4jDriver.AsyncSession();
        await session.ExecuteWriteAsync(async tx =>
        {
            var cursor = await tx.RunAsync(query, parameters);
            await cursor.SingleAsync();
            return 0;
        });
    }

    private static string? ValidateEventRequest(ShopEventRequest request)
    {
        if (request is null)
        {
            return "Request body is required.";
        }

        if (string.IsNullOrWhiteSpace(request.EventType))
        {
            return "EventType is required.";
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return "UserId is required.";
        }

        if (string.IsNullOrWhiteSpace(request.ProductId))
        {
            return "ProductId is required.";
        }

        try
        {
            _ = NormalizeEventType(request.EventType);
        }
        catch (ArgumentOutOfRangeException)
        {
            return "EventType must be one of: shown, viewed, liked, purchased.";
        }

        return null;
    }

    private static string NormalizeEventType(string eventType) =>
        eventType.Trim().ToLowerInvariant() switch
        {
            "shown" or "show" or "impression" => "shown",
            "viewed" or "view" or "opened" => "viewed",
            "liked" or "like" or "favorite" or "favourite" => "liked",
            "purchased" or "purchase" or "bought" or "buy" => "purchased",
            _ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, "Unsupported event type.")
        };

    private static DateTime NormalizeOccurredAt(DateTime? occurredAtUtc)
    {
        var timestamp = occurredAtUtc ?? DateTime.UtcNow;
        return timestamp.Kind switch
        {
            DateTimeKind.Utc => timestamp,
            DateTimeKind.Local => timestamp.ToUniversalTime(),
            _ => DateTime.SpecifyKind(timestamp, DateTimeKind.Utc)
        };
    }

    private static string SerializeMongoProducts(IReadOnlyCollection<BsonDocument> products)
    {
        var writerSettings = new JsonWriterSettings
        {
            OutputMode = JsonOutputMode.RelaxedExtendedJson
        };

        return new BsonArray(products).ToJson(writerSettings);
    }

    private sealed record EventDefinition(string RelationshipType, int EdgeType, bool IsProductToUser);
}

public sealed record ShopEventRequest(
    string EventType,
    string UserId,
    string ProductId,
    string? UserName = null,
    string? ProductName = null,
    List<string>? ProductTags = null,
    DateTime? OccurredAtUtc = null,
    int? Rating = null);

public sealed record ShopEventBatchRequest(
    List<ShopEventRequest> Events,
    bool ContinueOnError = false);

public sealed record ShopEventAcceptedResponse(
    string EventType,
    string UserId,
    string ProductId,
    DateTime OccurredAtUtc);

public sealed record ShopEventBatchResponse(
    int Processed,
    int Failed,
    IReadOnlyDictionary<string, int> Counts,
    IReadOnlyList<ShopEventBatchError> Errors);

public sealed record ShopEventBatchError(int Index, string Error);

public sealed record DtoSeedData(
    int UserCount = 30,
    int ProductCount = 50,
    int StoreCount = 10,
    double ViewedProb = 0.35,
    double LikedProb = 0.35,
    double PurchasedProb = 0.35,
    double BoughtTogetherProb = 0.35,
    double VisitedProb = 0.35,
    double QuantityProb = 0.35,
    double ShownProb = 0.35);
