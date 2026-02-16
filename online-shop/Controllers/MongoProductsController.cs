using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace ECommerce.Controller;

[ApiController]
[Route("api/mongo/products")]
public sealed class MongoProductsController : ControllerBase
{
    private readonly IMongoCollection<MongoProductDocument> _collection;
    private readonly ILogger<MongoProductsController> _logger;

    public MongoProductsController(IMongoDatabase mongoDatabase, ILogger<MongoProductsController> logger)
    {
        _collection = mongoDatabase.GetCollection<MongoProductDocument>("products");
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MongoProductResponse>>> GetAll(
        [FromQuery] string? query,
        [FromQuery] bool? isActive,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        var filterBuilder = Builders<MongoProductDocument>.Filter;
        var filters = new List<FilterDefinition<MongoProductDocument>>();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var escaped = Regex.Escape(query.Trim());
            var regex = new BsonRegularExpression(escaped, "i");

            filters.Add(filterBuilder.Or(
                filterBuilder.Regex(x => x.Sku, regex),
                filterBuilder.Regex(x => x.Name, regex),
                filterBuilder.Regex(x => x.Category, regex)));
        }

        if (isActive.HasValue)
        {
            filters.Add(filterBuilder.Eq(x => x.IsActive, isActive.Value));
        }

        var filter = filters.Count == 0 ? filterBuilder.Empty : filterBuilder.And(filters);

        var docs = await _collection
            .Find(filter)
            .SortByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Limit(take)
            .ToListAsync(cancellationToken);

        var response = docs.Select(MapToResponse).ToList();

        _logger.LogInformation(
            "Mongo products list requested. Query: {Query}, IsActive: {IsActive}, Skip: {Skip}, Take: {Take}, ResultCount: {ResultCount}.",
            query,
            isActive,
            skip,
            take,
            response.Count);

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MongoProductResponse>> GetById(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return BadRequest("Invalid MongoDB object id.");
        }

        var doc = await _collection
            .Find(x => x.Id == objectId)
            .FirstOrDefaultAsync(cancellationToken);

        if (doc is null)
        {
            _logger.LogWarning("Mongo product not found. Id: {ProductId}.", id);
            return NotFound($"Mongo product '{id}' was not found.");
        }

        return Ok(MapToResponse(doc));
    }

    [HttpPost]
    public async Task<ActionResult<MongoProductResponse>> Create(
        [FromBody] MongoCreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateCreateOrUpdateRequest(request.Sku, request.Name, request.Price);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var normalizedSku = request.Sku.Trim();
        var exists = await _collection
            .Find(x => x.Sku == normalizedSku)
            .AnyAsync(cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Create Mongo product rejected because SKU already exists. Sku: {Sku}.", normalizedSku);
            return Conflict($"Mongo product with SKU '{normalizedSku}' already exists.");
        }

        var now = DateTime.UtcNow;
        var doc = new MongoProductDocument
        {
            Sku = normalizedSku,
            Name = request.Name.Trim(),
            Category = NormalizeOptional(request.Category),
            Price = request.Price,
            IsActive = request.IsActive,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _collection.InsertOneAsync(doc, cancellationToken: cancellationToken);

        _logger.LogInformation("Mongo product created. Id: {ProductId}, Sku: {Sku}.", doc.Id, doc.Sku);
        return CreatedAtAction(nameof(GetById), new { id = doc.Id.ToString() }, MapToResponse(doc));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<MongoProductResponse>> Update(
        string id,
        [FromBody] MongoUpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return BadRequest("Invalid MongoDB object id.");
        }

        var validationError = ValidateCreateOrUpdateRequest(request.Sku, request.Name, request.Price);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var normalizedSku = request.Sku.Trim();
        var duplicateSkuExists = await _collection
            .Find(x => x.Id != objectId && x.Sku == normalizedSku)
            .AnyAsync(cancellationToken);

        if (duplicateSkuExists)
        {
            _logger.LogWarning(
                "Update Mongo product rejected because SKU already exists. ProductId: {ProductId}, Sku: {Sku}.",
                id,
                normalizedSku);

            return Conflict($"Mongo product with SKU '{normalizedSku}' already exists.");
        }

        var update = Builders<MongoProductDocument>.Update
            .Set(x => x.Sku, normalizedSku)
            .Set(x => x.Name, request.Name.Trim())
            .Set(x => x.Category, NormalizeOptional(request.Category))
            .Set(x => x.Price, request.Price)
            .Set(x => x.IsActive, request.IsActive)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<MongoProductDocument>
        {
            ReturnDocument = ReturnDocument.After
        };

        var updated = await _collection.FindOneAndUpdateAsync(x => x.Id == objectId, update, options, cancellationToken);

        if (updated is null)
        {
            _logger.LogWarning("Update Mongo product failed because entity was not found. ProductId: {ProductId}.", id);
            return NotFound($"Mongo product '{id}' was not found.");
        }

        _logger.LogInformation("Mongo product updated. ProductId: {ProductId}, Sku: {Sku}.", id, updated.Sku);
        return Ok(MapToResponse(updated));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return BadRequest("Invalid MongoDB object id.");
        }

        var deleted = await _collection.DeleteOneAsync(x => x.Id == objectId, cancellationToken);
        if (deleted.DeletedCount == 0)
        {
            _logger.LogWarning("Delete Mongo product failed because entity was not found. ProductId: {ProductId}.", id);
            return NotFound($"Mongo product '{id}' was not found.");
        }

        _logger.LogInformation("Mongo product deleted. ProductId: {ProductId}.", id);
        return NoContent();
    }

    [HttpPatch("{id}/active")]
    public async Task<ActionResult<MongoProductResponse>> SetActive(
        string id,
        [FromBody] MongoSetActiveRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return BadRequest("Invalid MongoDB object id.");
        }

        var update = Builders<MongoProductDocument>.Update
            .Set(x => x.IsActive, request.IsActive)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<MongoProductDocument>
        {
            ReturnDocument = ReturnDocument.After
        };

        var updated = await _collection.FindOneAndUpdateAsync(x => x.Id == objectId, update, options, cancellationToken);

        if (updated is null)
        {
            _logger.LogWarning("Set-active Mongo product failed because entity was not found. ProductId: {ProductId}.", id);
            return NotFound($"Mongo product '{id}' was not found.");
        }

        _logger.LogInformation("Mongo product active flag updated. ProductId: {ProductId}, IsActive: {IsActive}.", id, request.IsActive);
        return Ok(MapToResponse(updated));
    }

    [HttpPost("upsert-by-sku/{sku}")]
    public async Task<ActionResult<MongoProductResponse>> UpsertBySku(
        string sku,
        [FromBody] MongoUpsertBySkuRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedSku = sku?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSku))
        {
            return BadRequest("SKU is required in route.");
        }

        var validationError = ValidateCreateOrUpdateRequest(normalizedSku, request.Name, request.Price);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var now = DateTime.UtcNow;
        var filter = Builders<MongoProductDocument>.Filter.Eq(x => x.Sku, normalizedSku);
        var update = Builders<MongoProductDocument>.Update
            .Set(x => x.Name, request.Name.Trim())
            .Set(x => x.Category, NormalizeOptional(request.Category))
            .Set(x => x.Price, request.Price)
            .Set(x => x.IsActive, request.IsActive)
            .Set(x => x.UpdatedAtUtc, now)
            .SetOnInsert(x => x.Sku, normalizedSku)
            .SetOnInsert(x => x.CreatedAtUtc, now);

        var options = new FindOneAndUpdateOptions<MongoProductDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var result = await _collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);

        _logger.LogInformation(
            "Mongo product upserted by SKU. Sku: {Sku}, ProductId: {ProductId}.",
            normalizedSku,
            result.Id);

        return Ok(MapToResponse(result));
    }

    [HttpGet("recent")]
    public async Task<ActionResult<IReadOnlyList<MongoProductResponse>>> GetRecent(
        [FromQuery] int minutes = 60,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        minutes = Math.Clamp(minutes, 1, 60 * 24 * 30);
        take = Math.Clamp(take, 1, 500);

        var border = DateTime.UtcNow.AddMinutes(-minutes);
        var docs = await _collection
            .Find(x => x.CreatedAtUtc >= border)
            .SortByDescending(x => x.CreatedAtUtc)
            .Limit(take)
            .ToListAsync(cancellationToken);

        var response = docs.Select(MapToResponse).ToList();

        _logger.LogInformation(
            "Recent Mongo products requested. Minutes: {Minutes}, Take: {Take}, ResultCount: {ResultCount}.",
            minutes,
            take,
            response.Count);

        return Ok(response);
    }

    [HttpGet("analytics/by-category")]
    public async Task<ActionResult<IReadOnlyList<MongoCategoryStatsResponse>>> GetCategoryStats(
        CancellationToken cancellationToken = default)
    {
        // Агрегация выполняется в MongoDB, чтобы не вытаскивать весь набор в приложение.
        var aggregation = _collection.Aggregate()
            .Group(new BsonDocument
            {
                { "_id", "$category" },
                { "total", new BsonDocument("$sum", 1) },
                {
                    "active",
                    new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray { "$isActive", 1, 0 }))
                },
                { "avgPrice", new BsonDocument("$avg", "$price") }
            });

        var docs = await aggregation.ToListAsync(cancellationToken);

        var response = docs
            .Select(x => new MongoCategoryStatsResponse(
                ReadCategory(x.GetValue("_id", BsonNull.Value)),
                x.GetValue("total", 0).ToInt32(),
                x.GetValue("active", 0).ToInt32(),
                ReadDecimal(x.GetValue("avgPrice", 0))))
            .OrderByDescending(x => x.TotalProducts)
            .ThenBy(x => x.Category)
            .ToList();

        _logger.LogInformation("Mongo category stats requested. ResultCount: {ResultCount}.", response.Count);
        return Ok(response);
    }

    private static MongoProductResponse MapToResponse(MongoProductDocument doc) =>
        new(
            doc.Id.ToString(),
            doc.Sku,
            doc.Name,
            doc.Category,
            doc.Price,
            doc.IsActive,
            doc.CreatedAtUtc,
            doc.UpdatedAtUtc);

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string? ValidateCreateOrUpdateRequest(string? sku, string? name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return "SKU is required.";
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required.";
        }

        if (price < 0)
        {
            return "Price cannot be negative.";
        }

        return null;
    }

    private static decimal ReadDecimal(BsonValue value)
    {
        return value.BsonType switch
        {
            BsonType.Decimal128 => Decimal128.ToDecimal(value.AsDecimal128),
            BsonType.Double => Convert.ToDecimal(value.AsDouble),
            BsonType.Int32 => value.AsInt32,
            BsonType.Int64 => value.AsInt64,
            BsonType.String when decimal.TryParse(value.AsString, out var parsed) => parsed,
            _ => 0m
        };
    }

    private static string ReadCategory(BsonValue value)
    {
        if (value.IsBsonNull)
        {
            return "(none)";
        }

        return value.ToString() ?? "(none)";
    }
}

public sealed class MongoProductDocument
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("sku")]
    public string Sku { get; set; } = null!;

    [BsonElement("name")]
    public string Name { get; set; } = null!;

    [BsonElement("category")]
    public string? Category { get; set; }

    [BsonElement("price")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Price { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; }

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed record MongoCreateProductRequest(string Sku, string Name, string? Category, decimal Price, bool IsActive);

public sealed record MongoUpdateProductRequest(string Sku, string Name, string? Category, decimal Price, bool IsActive);

public sealed record MongoUpsertBySkuRequest(string Name, string? Category, decimal Price, bool IsActive);

public sealed record MongoSetActiveRequest(bool IsActive);

public sealed record MongoProductResponse(
    string Id,
    string Sku,
    string Name,
    string? Category,
    decimal Price,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record MongoCategoryStatsResponse(
    string Category,
    int TotalProducts,
    int ActiveProducts,
    decimal AveragePrice);
