using ECommerce.Data;
using ECommerce.Data.Entities.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ECommerce.Controller;

[ApiController]
[Route("api/postgres/products")]
public sealed class PostgresProductsController : ControllerBase
{
    private readonly ECommerceDbContext _dbContext;
    private readonly ILogger<PostgresProductsController> _logger;

    public PostgresProductsController(ECommerceDbContext dbContext, ILogger<PostgresProductsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductListItemResponse>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] Guid? brandId,
        [FromQuery] Guid? categoryId,
        [FromQuery] ProductStatus? status,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 200);

        var query = _dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Name, pattern) || EF.Functions.ILike(x.SkuBase, pattern));
        }

        if (brandId.HasValue)
        {
            query = query.Where(x => x.BrandId == brandId.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new ProductListItemResponse(
                x.ProductId,
                x.SkuBase,
                x.Name,
                x.Status,
                x.BrandId,
                x.Brand != null ? x.Brand.Name : null,
                x.CategoryId,
                x.Category != null ? x.Category.Name : null,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "PostgreSQL products list requested. Search: {Search}, BrandId: {BrandId}, CategoryId: {CategoryId}, Status: {Status}, Skip: {Skip}, Take: {Take}, ResultCount: {ResultCount}.",
            search,
            brandId,
            categoryId,
            status,
            skip,
            take,
            items.Count);

        return Ok(items);
    }

    [HttpGet("{productId:guid}")]
    public async Task<ActionResult<ProductDetailsResponse>> GetById(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.Products
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => new ProductDetailsResponse(
                x.ProductId,
                x.SkuBase,
                x.Name,
                x.Status,
                x.AttrsJson,
                x.BrandId,
                x.Brand != null ? x.Brand.Name : null,
                x.CategoryId,
                x.Category != null ? x.Category.Name : null,
                x.CreatedAt,
                x.Variants.Count(),
                x.Media.Count()))
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            _logger.LogWarning("Product not found. ProductId: {ProductId}.", productId);
            return NotFound($"Product '{productId}' was not found.");
        }

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDetailsResponse>> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateReferencesAndPayloadAsync(
            request.BrandId,
            request.CategoryId,
            request.SkuBase,
            request.Name,
            request.AttrsJson,
            cancellationToken);

        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var entity = new Product
        {
            ProductId = Guid.NewGuid(),
            BrandId = request.BrandId,
            CategoryId = request.CategoryId,
            SkuBase = request.SkuBase.Trim(),
            Name = request.Name.Trim(),
            Status = request.Status,
            AttrsJson = string.IsNullOrWhiteSpace(request.AttrsJson) ? "{}" : request.AttrsJson.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Products.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Product created. ProductId: {ProductId}, SkuBase: {SkuBase}, Status: {Status}.",
            entity.ProductId,
            entity.SkuBase,
            entity.Status);

        var response = await BuildDetailsResponseAsync(entity.ProductId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { productId = entity.ProductId }, response!);
    }

    [HttpPut("{productId:guid}")]
    public async Task<ActionResult<ProductDetailsResponse>> Update(
        Guid productId,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Products
            .FirstOrDefaultAsync(x => x.ProductId == productId, cancellationToken);

        if (entity is null)
        {
            _logger.LogWarning("Update product failed because entity was not found. ProductId: {ProductId}.", productId);
            return NotFound($"Product '{productId}' was not found.");
        }

        var validationError = await ValidateReferencesAndPayloadAsync(
            request.BrandId,
            request.CategoryId,
            request.SkuBase,
            request.Name,
            request.AttrsJson,
            cancellationToken);

        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        entity.BrandId = request.BrandId;
        entity.CategoryId = request.CategoryId;
        entity.SkuBase = request.SkuBase.Trim();
        entity.Name = request.Name.Trim();
        entity.Status = request.Status;
        entity.AttrsJson = string.IsNullOrWhiteSpace(request.AttrsJson) ? "{}" : request.AttrsJson.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Product updated. ProductId: {ProductId}, SkuBase: {SkuBase}, Status: {Status}.",
            entity.ProductId,
            entity.SkuBase,
            entity.Status);

        var response = await BuildDetailsResponseAsync(entity.ProductId, cancellationToken);
        return Ok(response);
    }

    [HttpPatch("{productId:guid}/status")]
    public async Task<ActionResult<ProductDetailsResponse>> UpdateStatus(
        Guid productId,
        [FromBody] UpdateProductStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Products
            .FirstOrDefaultAsync(x => x.ProductId == productId, cancellationToken);

        if (entity is null)
        {
            _logger.LogWarning("Update product status failed because entity was not found. ProductId: {ProductId}.", productId);
            return NotFound($"Product '{productId}' was not found.");
        }

        entity.Status = request.Status;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product status updated. ProductId: {ProductId}, Status: {Status}.", productId, request.Status);

        var response = await BuildDetailsResponseAsync(productId, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{productId:guid}")]
    public async Task<IActionResult> Delete(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Products
            .FirstOrDefaultAsync(x => x.ProductId == productId, cancellationToken);

        if (entity is null)
        {
            _logger.LogWarning("Delete product failed because entity was not found. ProductId: {ProductId}.", productId);
            return NotFound($"Product '{productId}' was not found.");
        }

        var hasVariants = await _dbContext.Variants
            .AsNoTracking()
            .AnyAsync(x => x.ProductId == productId, cancellationToken);

        if (hasVariants)
        {
            _logger.LogWarning("Delete product rejected because product has variants. ProductId: {ProductId}.", productId);
            return Conflict("Product cannot be deleted while it has variants.");
        }

        _dbContext.Products.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product deleted. ProductId: {ProductId}.", productId);
        return NoContent();
    }

    [HttpGet("active")]
    public async Task<ActionResult<IReadOnlyList<ProductListItemResponse>>> GetActive(
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);

        var items = await _dbContext.Products
            .AsNoTracking()
            .Where(x => x.Status == ProductStatus.Active)
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .Select(x => new ProductListItemResponse(
                x.ProductId,
                x.SkuBase,
                x.Name,
                x.Status,
                x.BrandId,
                x.Brand != null ? x.Brand.Name : null,
                x.CategoryId,
                x.Category != null ? x.Category.Name : null,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Active products requested. Take: {Take}, ResultCount: {ResultCount}.", take, items.Count);
        return Ok(items);
    }

    [HttpGet("stats/by-status")]
    public async Task<ActionResult<IReadOnlyList<ProductStatusStatsResponse>>> GetStatsByStatus(
        CancellationToken cancellationToken = default)
    {
        // Отдельный endpoint для быстрых операционных метрик по каталогу.
        var stats = await _dbContext.Products
            .AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(group => new ProductStatusStatsResponse(group.Key, group.Count()))
            .OrderBy(x => x.Status)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Product status statistics requested. ResultCount: {ResultCount}.", stats.Count);
        return Ok(stats);
    }

    private async Task<ProductDetailsResponse?> BuildDetailsResponseAsync(Guid productId, CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => new ProductDetailsResponse(
                x.ProductId,
                x.SkuBase,
                x.Name,
                x.Status,
                x.AttrsJson,
                x.BrandId,
                x.Brand != null ? x.Brand.Name : null,
                x.CategoryId,
                x.Category != null ? x.Category.Name : null,
                x.CreatedAt,
                x.Variants.Count(),
                x.Media.Count()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<string?> ValidateReferencesAndPayloadAsync(
        Guid? brandId,
        Guid? categoryId,
        string? skuBase,
        string? name,
        string? attrsJson,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(skuBase))
        {
            return "SkuBase is required.";
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required.";
        }

        if (brandId.HasValue)
        {
            var brandExists = await _dbContext.Brands
                .AsNoTracking()
                .AnyAsync(x => x.BrandId == brandId.Value, cancellationToken);

            if (!brandExists)
            {
                return $"Brand '{brandId}' was not found.";
            }
        }

        if (categoryId.HasValue)
        {
            var categoryExists = await _dbContext.Categories
                .AsNoTracking()
                .AnyAsync(x => x.CategoryId == categoryId.Value, cancellationToken);

            if (!categoryExists)
            {
                return $"Category '{categoryId}' was not found.";
            }
        }

        if (!string.IsNullOrWhiteSpace(attrsJson) && !IsValidJson(attrsJson))
        {
            return "AttrsJson must contain valid JSON.";
        }

        return null;
    }

    private static bool IsValidJson(string payload)
    {
        try
        {
            _ = JsonDocument.Parse(payload);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public sealed record CreateProductRequest(
    Guid? BrandId,
    Guid? CategoryId,
    string SkuBase,
    string Name,
    ProductStatus Status,
    string? AttrsJson);

public sealed record UpdateProductRequest(
    Guid? BrandId,
    Guid? CategoryId,
    string SkuBase,
    string Name,
    ProductStatus Status,
    string? AttrsJson);

public sealed record UpdateProductStatusRequest(ProductStatus Status);

public sealed record ProductListItemResponse(
    Guid ProductId,
    string SkuBase,
    string Name,
    ProductStatus Status,
    Guid? BrandId,
    string? BrandName,
    Guid? CategoryId,
    string? CategoryName,
    DateTimeOffset CreatedAt);

public sealed record ProductDetailsResponse(
    Guid ProductId,
    string SkuBase,
    string Name,
    ProductStatus Status,
    string AttrsJson,
    Guid? BrandId,
    string? BrandName,
    Guid? CategoryId,
    string? CategoryName,
    DateTimeOffset CreatedAt,
    int VariantsCount,
    int MediaCount);

public sealed record ProductStatusStatsResponse(ProductStatus Status, int Count);
