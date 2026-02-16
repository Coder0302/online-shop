using ECommerce.Data;
using ECommerce.Data.Entities.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Controller;

[ApiController]
[Route("api/postgres/brands")]
public sealed class PostgresBrandsController : ControllerBase
{
    private readonly ECommerceDbContext _dbContext;
    private readonly ILogger<PostgresBrandsController> _logger;

    public PostgresBrandsController(ECommerceDbContext dbContext, ILogger<PostgresBrandsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BrandListItemResponse>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 200);

        var query = _dbContext.Brands.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Name, pattern));
        }

        var items = await query
            .OrderBy(x => x.Name)
            .Skip(skip)
            .Take(take)
            .Select(x => new BrandListItemResponse(x.BrandId, x.Name))
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "PostgreSQL brands list requested. Search: {Search}, Skip: {Skip}, Take: {Take}, ResultCount: {ResultCount}.",
            search,
            skip,
            take,
            items.Count);

        return Ok(items);
    }

    [HttpGet("{brandId:guid}")]
    public async Task<ActionResult<BrandDetailsResponse>> GetById(
        Guid brandId,
        CancellationToken cancellationToken = default)
    {
        var result = await _dbContext.Brands
            .AsNoTracking()
            .Where(x => x.BrandId == brandId)
            .Select(x => new BrandDetailsResponse(
                x.BrandId,
                x.Name,
                x.Products.Count()))
            .FirstOrDefaultAsync(cancellationToken);

        if (result is null)
        {
            _logger.LogWarning("Brand not found. BrandId: {BrandId}.", brandId);
            return NotFound($"Brand '{brandId}' was not found.");
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<BrandDetailsResponse>> Create(
        [FromBody] CreateBrandRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return BadRequest("Brand name is required.");
        }

        var exists = await _dbContext.Brands
            .AsNoTracking()
            .AnyAsync(x => x.Name == normalizedName, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Create brand rejected because name already exists. Name: {BrandName}.", normalizedName);
            return Conflict($"Brand '{normalizedName}' already exists.");
        }

        var entity = new Brand
        {
            BrandId = Guid.NewGuid(),
            Name = normalizedName
        };

        _dbContext.Brands.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Brand created. BrandId: {BrandId}, Name: {BrandName}.", entity.BrandId, entity.Name);

        var response = new BrandDetailsResponse(entity.BrandId, entity.Name, 0);
        return CreatedAtAction(nameof(GetById), new { brandId = entity.BrandId }, response);
    }

    [HttpPut("{brandId:guid}")]
    public async Task<ActionResult<BrandDetailsResponse>> Update(
        Guid brandId,
        [FromBody] UpdateBrandRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return BadRequest("Brand name is required.");
        }

        var entity = await _dbContext.Brands
            .FirstOrDefaultAsync(x => x.BrandId == brandId, cancellationToken);

        if (entity is null)
        {
            _logger.LogWarning("Update brand failed because entity was not found. BrandId: {BrandId}.", brandId);
            return NotFound($"Brand '{brandId}' was not found.");
        }

        var duplicateExists = await _dbContext.Brands
            .AsNoTracking()
            .AnyAsync(x => x.BrandId != brandId && x.Name == normalizedName, cancellationToken);

        if (duplicateExists)
        {
            _logger.LogWarning(
                "Update brand rejected because target name already exists. BrandId: {BrandId}, Name: {BrandName}.",
                brandId,
                normalizedName);

            return Conflict($"Brand '{normalizedName}' already exists.");
        }

        entity.Name = normalizedName;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var productCount = await _dbContext.Products
            .AsNoTracking()
            .CountAsync(x => x.BrandId == entity.BrandId, cancellationToken);

        _logger.LogInformation("Brand updated. BrandId: {BrandId}, NewName: {BrandName}.", entity.BrandId, entity.Name);

        return Ok(new BrandDetailsResponse(entity.BrandId, entity.Name, productCount));
    }

    [HttpDelete("{brandId:guid}")]
    public async Task<IActionResult> Delete(
        Guid brandId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Brands
            .FirstOrDefaultAsync(x => x.BrandId == brandId, cancellationToken);

        if (entity is null)
        {
            _logger.LogWarning("Delete brand failed because entity was not found. BrandId: {BrandId}.", brandId);
            return NotFound($"Brand '{brandId}' was not found.");
        }

        var productCount = await _dbContext.Products
            .AsNoTracking()
            .CountAsync(x => x.BrandId == brandId, cancellationToken);

        if (productCount > 0)
        {
            _logger.LogWarning(
                "Delete brand rejected because it has related products. BrandId: {BrandId}, ProductCount: {ProductCount}.",
                brandId,
                productCount);

            return Conflict("Brand cannot be deleted while it is used by products.");
        }

        _dbContext.Brands.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Brand deleted. BrandId: {BrandId}.", brandId);
        return NoContent();
    }

    [HttpGet("{brandId:guid}/products-count")]
    public async Task<ActionResult<BrandProductsCountResponse>> GetProductsCount(
        Guid brandId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.Brands
            .AsNoTracking()
            .AnyAsync(x => x.BrandId == brandId, cancellationToken);

        if (!exists)
        {
            _logger.LogWarning("Products-count failed because brand was not found. BrandId: {BrandId}.", brandId);
            return NotFound($"Brand '{brandId}' was not found.");
        }

        var count = await _dbContext.Products
            .AsNoTracking()
            .CountAsync(x => x.BrandId == brandId, cancellationToken);

        _logger.LogInformation("Products-count returned for brand. BrandId: {BrandId}, ProductCount: {ProductCount}.", brandId, count);
        return Ok(new BrandProductsCountResponse(brandId, count));
    }

    [HttpGet("top-by-products")]
    public async Task<ActionResult<IReadOnlyList<TopBrandByProductsResponse>>> GetTopByProducts(
        [FromQuery] int take = 10,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 100);

        var items = await _dbContext.Brands
            .AsNoTracking()
            .Select(x => new TopBrandByProductsResponse(
                x.BrandId,
                x.Name,
                x.Products.Count()))
            .OrderByDescending(x => x.ProductsCount)
            .ThenBy(x => x.BrandName)
            .Take(take)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Top brands by products requested. Take: {Take}, ResultCount: {ResultCount}.", take, items.Count);
        return Ok(items);
    }
}

public sealed record CreateBrandRequest(string Name);

public sealed record UpdateBrandRequest(string Name);

public sealed record BrandListItemResponse(Guid BrandId, string Name);

public sealed record BrandDetailsResponse(Guid BrandId, string Name, int ProductsCount);

public sealed record BrandProductsCountResponse(Guid BrandId, int ProductsCount);

public sealed record TopBrandByProductsResponse(Guid BrandId, string BrandName, int ProductsCount);
