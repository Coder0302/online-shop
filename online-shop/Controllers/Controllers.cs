using ECommerce.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
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
    private readonly ILogger<ShopController> _logger;

    public ShopController(
        IMongoDatabase mongoDatabase,
        IDatabase redisDatabase,
        ECommerceDbContext dbContext,
        ILogger<ShopController> logger)
    {
        _mongoDatabase = mongoDatabase;
        _redisDatabase = redisDatabase;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(CancellationToken cancellationToken)
    {
        // 1) Быстрый путь: пробуем вернуть уже подготовленный JSON из Redis.
        var cachedProducts = await _redisDatabase.StringGetAsync(ProductsCacheKey);
        if (cachedProducts.HasValue)
        {
            _logger.LogInformation("Products returned from Redis cache.");
            return Content(cachedProducts.ToString(), "application/json");
        }

        // 2) Если кэш пуст, тянем из MongoDB и кладем результат обратно в Redis.
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

        // 3) Последний fallback: PostgreSQL (первый доступный продукт).
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

    private static string SerializeMongoProducts(IReadOnlyCollection<BsonDocument> products)
    {
        var writerSettings = new JsonWriterSettings
        {
            OutputMode = JsonOutputMode.RelaxedExtendedJson
        };

        return new BsonArray(products).ToJson(writerSettings);
    }
}
