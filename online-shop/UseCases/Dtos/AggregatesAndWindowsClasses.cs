using Microsoft.EntityFrameworkCore;

namespace ECommerce.App.UseCases.Dtos;

public sealed record ActiveProductsByCategoryDto(Guid? CategoryId, string CategoryName, string? Slug, int ActiveProductsCount);
public sealed record SkusPerProductDto(Guid ProductId, string ProductName, int SkusCount);
public sealed record AvailableStockPerVariantDto(Guid VariantId, int QtyAvailableSum);
public sealed record AvgCurrentPriceByBrandDto(Guid? BrandId, string BrandName, decimal? AvgPrice);
public sealed record CartTotalsByCustomerDto(Guid? CustomerId, decimal Total);