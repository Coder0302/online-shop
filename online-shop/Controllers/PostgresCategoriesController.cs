using ECommerce.Data;
using ECommerce.Data.Entities.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace ECommerce.Controller;

[ApiController]
[Route("api/postgres/categories")]
public sealed class PostgresCategoriesController : ControllerBase
{
    private static readonly Regex SpacesRegex = new("\\s+", RegexOptions.Compiled);

    private readonly ECommerceDbContext _dbContext;
    private readonly ILogger<PostgresCategoriesController> _logger;

    public PostgresCategoriesController(ECommerceDbContext dbContext, ILogger<PostgresCategoriesController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryListItemResponse>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] Guid? parentId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 200);

        var query = _dbContext.Categories.AsNoTracking();

        if (parentId.HasValue)
        {
            query = query.Where(x => x.ParentId == parentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Name, pattern) || EF.Functions.ILike(x.Slug, pattern));
        }

        var items = await query
            .OrderBy(x => x.Name)
            .Skip(skip)
            .Take(take)
            .Select(x => new CategoryListItemResponse(
                x.CategoryId,
                x.ParentId,
                x.Slug,
                x.Name))
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "PostgreSQL categories list requested. Search: {Search}, ParentId: {ParentId}, Skip: {Skip}, Take: {Take}, ResultCount: {ResultCount}.",
            search,
            parentId,
            skip,
            take,
            items.Count);

        return Ok(items);
    }

    [HttpGet("{categoryId:guid}")]
    public async Task<ActionResult<CategoryDetailsResponse>> GetById(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.CategoryId == categoryId)
            .Select(x => new CategoryDetailsResponse(
                x.CategoryId,
                x.ParentId,
                x.Slug,
                x.Name,
                x.Children.Count(),
                x.Products.Count()))
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            _logger.LogWarning("Category not found. CategoryId: {CategoryId}.", categoryId);
            return NotFound($"Category '{categoryId}' was not found.");
        }

        return Ok(item);
    }

    [HttpGet("by-slug/{slug}")]
    public async Task<ActionResult<CategoryDetailsResponse>> GetBySlug(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = NormalizeSlug(slug);
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return BadRequest("Slug is required.");
        }

        var item = await _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.Slug == normalizedSlug)
            .Select(x => new CategoryDetailsResponse(
                x.CategoryId,
                x.ParentId,
                x.Slug,
                x.Name,
                x.Children.Count(),
                x.Products.Count()))
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            _logger.LogWarning("Category by slug not found. Slug: {Slug}.", normalizedSlug);
            return NotFound($"Category slug '{normalizedSlug}' was not found.");
        }

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDetailsResponse>> Create(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = request.Name?.Trim();
        var normalizedSlug = NormalizeSlug(request.Slug);

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return BadRequest("Category name is required.");
        }

        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return BadRequest("Category slug is required.");
        }

        if (request.ParentId.HasValue)
        {
            var parentExists = await _dbContext.Categories
                .AsNoTracking()
                .AnyAsync(x => x.CategoryId == request.ParentId.Value, cancellationToken);

            if (!parentExists)
            {
                return BadRequest($"Parent category '{request.ParentId}' was not found.");
            }
        }

        var slugExists = await _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(x => x.Slug == normalizedSlug, cancellationToken);

        if (slugExists)
        {
            _logger.LogWarning("Create category rejected because slug already exists. Slug: {Slug}.", normalizedSlug);
            return Conflict($"Category slug '{normalizedSlug}' already exists.");
        }

        var entity = new Category
        {
            CategoryId = Guid.NewGuid(),
            ParentId = request.ParentId,
            Slug = normalizedSlug,
            Name = normalizedName
        };

        _dbContext.Categories.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Category created. CategoryId: {CategoryId}, Slug: {Slug}, ParentId: {ParentId}.",
            entity.CategoryId,
            entity.Slug,
            entity.ParentId);

        var response = new CategoryDetailsResponse(
            entity.CategoryId,
            entity.ParentId,
            entity.Slug,
            entity.Name,
            0,
            0);

        return CreatedAtAction(nameof(GetById), new { categoryId = entity.CategoryId }, response);
    }

    [HttpPut("{categoryId:guid}")]
    public async Task<ActionResult<CategoryDetailsResponse>> Update(
        Guid categoryId,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = request.Name?.Trim();
        var normalizedSlug = NormalizeSlug(request.Slug);

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return BadRequest("Category name is required.");
        }

        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return BadRequest("Category slug is required.");
        }

        var entity = await _dbContext.Categories
            .FirstOrDefaultAsync(x => x.CategoryId == categoryId, cancellationToken);

        if (entity is null)
        {
            _logger.LogWarning("Update category failed because entity was not found. CategoryId: {CategoryId}.", categoryId);
            return NotFound($"Category '{categoryId}' was not found.");
        }

        if (request.ParentId == categoryId)
        {
            return BadRequest("Category cannot be parent of itself.");
        }

        if (request.ParentId.HasValue)
        {
            var parentExists = await _dbContext.Categories
                .AsNoTracking()
                .AnyAsync(x => x.CategoryId == request.ParentId.Value, cancellationToken);

            if (!parentExists)
            {
                return BadRequest($"Parent category '{request.ParentId}' was not found.");
            }
        }

        var slugExists = await _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(x => x.CategoryId != categoryId && x.Slug == normalizedSlug, cancellationToken);

        if (slugExists)
        {
            _logger.LogWarning(
                "Update category rejected because slug already exists. CategoryId: {CategoryId}, Slug: {Slug}.",
                categoryId,
                normalizedSlug);

            return Conflict($"Category slug '{normalizedSlug}' already exists.");
        }

        entity.ParentId = request.ParentId;
        entity.Slug = normalizedSlug;
        entity.Name = normalizedName;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var childrenCount = await _dbContext.Categories
            .AsNoTracking()
            .CountAsync(x => x.ParentId == categoryId, cancellationToken);

        var productsCount = await _dbContext.Products
            .AsNoTracking()
            .CountAsync(x => x.CategoryId == categoryId, cancellationToken);

        _logger.LogInformation("Category updated. CategoryId: {CategoryId}, Slug: {Slug}.", entity.CategoryId, entity.Slug);

        return Ok(new CategoryDetailsResponse(
            entity.CategoryId,
            entity.ParentId,
            entity.Slug,
            entity.Name,
            childrenCount,
            productsCount));
    }

    [HttpDelete("{categoryId:guid}")]
    public async Task<IActionResult> Delete(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Categories
            .FirstOrDefaultAsync(x => x.CategoryId == categoryId, cancellationToken);

        if (entity is null)
        {
            _logger.LogWarning("Delete category failed because entity was not found. CategoryId: {CategoryId}.", categoryId);
            return NotFound($"Category '{categoryId}' was not found.");
        }

        var childrenCount = await _dbContext.Categories
            .AsNoTracking()
            .CountAsync(x => x.ParentId == categoryId, cancellationToken);

        if (childrenCount > 0)
        {
            _logger.LogWarning(
                "Delete category rejected because category has child categories. CategoryId: {CategoryId}, ChildrenCount: {ChildrenCount}.",
                categoryId,
                childrenCount);

            return Conflict("Category cannot be deleted while it has child categories.");
        }

        var productsCount = await _dbContext.Products
            .AsNoTracking()
            .CountAsync(x => x.CategoryId == categoryId, cancellationToken);

        if (productsCount > 0)
        {
            _logger.LogWarning(
                "Delete category rejected because category has products. CategoryId: {CategoryId}, ProductsCount: {ProductsCount}.",
                categoryId,
                productsCount);

            return Conflict("Category cannot be deleted while it has products.");
        }

        _dbContext.Categories.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Category deleted. CategoryId: {CategoryId}.", categoryId);
        return NoContent();
    }

    [HttpGet("{categoryId:guid}/children")]
    public async Task<ActionResult<IReadOnlyList<CategoryListItemResponse>>> GetChildren(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var parentExists = await _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(x => x.CategoryId == categoryId, cancellationToken);

        if (!parentExists)
        {
            _logger.LogWarning("Children request failed because parent category was not found. CategoryId: {CategoryId}.", categoryId);
            return NotFound($"Category '{categoryId}' was not found.");
        }

        var children = await _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.ParentId == categoryId)
            .OrderBy(x => x.Name)
            .Select(x => new CategoryListItemResponse(
                x.CategoryId,
                x.ParentId,
                x.Slug,
                x.Name))
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Category children requested. CategoryId: {CategoryId}, ResultCount: {ResultCount}.", categoryId, children.Count);
        return Ok(children);
    }

    [HttpGet("tree-summary")]
    public async Task<ActionResult<IReadOnlyList<CategoryTreeSummaryResponse>>> GetTreeSummary(
        CancellationToken cancellationToken = default)
    {
        // Это легковесная сводка по дереву категорий без рекурсивного обхода на уровне API.
        var summary = await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new CategoryTreeSummaryResponse(
                x.CategoryId,
                x.ParentId,
                x.Slug,
                x.Name,
                x.Children.Count(),
                x.Products.Count()))
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Category tree summary requested. ResultCount: {ResultCount}.", summary.Count);
        return Ok(summary);
    }

    private static string NormalizeSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return string.Empty;
        }

        var normalized = slug.Trim().ToLowerInvariant();
        normalized = SpacesRegex.Replace(normalized, "-");

        return normalized;
    }
}

public sealed record CreateCategoryRequest(Guid? ParentId, string Slug, string Name);

public sealed record UpdateCategoryRequest(Guid? ParentId, string Slug, string Name);

public sealed record CategoryListItemResponse(Guid CategoryId, Guid? ParentId, string Slug, string Name);

public sealed record CategoryDetailsResponse(
    Guid CategoryId,
    Guid? ParentId,
    string Slug,
    string Name,
    int ChildrenCount,
    int ProductsCount);

public sealed record CategoryTreeSummaryResponse(
    Guid CategoryId,
    Guid? ParentId,
    string Slug,
    string Name,
    int ChildrenCount,
    int ProductsCount);
