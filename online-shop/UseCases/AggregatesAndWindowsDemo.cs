using ECommerce.App.UseCases.Dtos;
using ECommerce.Data;
using ECommerce.Data.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.App.UseCases;

public sealed class AggregateAndWindowsDemo(ECommerceDbContext db)
{
    private readonly ECommerceDbContext _db = db;

    // ------------- АГРЕГИРУЮЩИЕ ЗАПРОСЫ (5 шт.) -------------

    /// 1) Кол-во АКТИВНЫХ товаров по категориям
    public async Task<IReadOnlyList<ActiveProductsByCategoryDto>> ActiveProductsByCategoryAsync(CancellationToken ct = default)
    {
        var q =
            from p in _db.Products
            where p.Status == ProductStatus.Active
            join c in _db.Categories on p.CategoryId equals c.CategoryId into cc
            from c in cc.DefaultIfEmpty()
            group p by new { c.CategoryId, c.Name, c.Slug } into g
            orderby g.Key.Name
            select new ActiveProductsByCategoryDto(
                g.Key.CategoryId,
                g.Key.Name ?? "(без категории)",
                g.Key.Slug,
                g.Count()
            );

        return await q.AsNoTracking().ToListAsync(ct);
    }

    /// 2) Кол-во SKU (Variants) по каждому товару
    public async Task<IReadOnlyList<SkusPerProductDto>> SkusPerProductAsync(CancellationToken ct = default)
    {
        var q =
        from p in _db.Products
        orderby p.Name
        select new SkusPerProductDto(
            p.ProductId,
            p.Name,
            p.Variants.Count()
        );

        return await q.AsNoTracking().ToListAsync(ct);
    }

    /// 3) Доступный остаток по каждому варианту (сумма по складам)
    public async Task<IReadOnlyList<AvailableStockPerVariantDto>> AvailableStockPerVariantAsync(CancellationToken ct = default)
    {
        var q =
            from si in _db.StockItems
            group si by si.VariantId into g
            select new AvailableStockPerVariantDto(
                g.Key,
                g.Sum(x => Math.Max(x.QtyOnHand - x.QtyReserved, 0))
            );

        return await q.AsNoTracking().ToListAsync(ct);
    }

    /// 4) Средняя ТЕКУЩАЯ цена по бренду в заданном прайс-листе
    public async Task<IReadOnlyList<AvgCurrentPriceByBrandDto>> AvgCurrentPriceByBrandAsync(
        string priceListCode, DateTimeOffset? at = null, CancellationToken ct = default)
    {
        var now = at ?? DateTimeOffset.UtcNow;

        var q =
            from pr in _db.Prices
            join pl in _db.PriceLists on pr.PriceListId equals pl.PriceListId
            where pl.Code == priceListCode
               && pr.ValidFrom <= now
               && (pr.ValidTo == null || now < pr.ValidTo)
            join v in _db.Variants on pr.VariantId equals v.VariantId
            join p in _db.Products on v.ProductId equals p.ProductId
            join b in _db.Brands on p.BrandId equals b.BrandId into bb
            from b in bb.DefaultIfEmpty()
            group pr by new { b.BrandId, BrandName = (string?)b.Name } into g
            orderby g.Key.BrandName
            select new AvgCurrentPriceByBrandDto(
                g.Key.BrandId,
                g.Key.BrandName ?? "(без бренда)",
                g.Average(x => x.Amount)
            );

        return await q.AsNoTracking().ToListAsync(ct);
    }

    /// 5) Сумма корзин по клиенту (текущая стоимость корзины = sum(qty * price_snapshot))
    public async Task<IReadOnlyList<CartTotalsByCustomerDto>> CartTotalsByCustomerAsync(CancellationToken ct = default)
    {
        var cartTotals =
            from ci in _db.CartItems
            group new { ci } by ci.CartId into g
            select new { CartId = g.Key, Total = g.Sum(x => x.ci.Qty * x.ci.PriceSnapshot) };

        var q =
            from c in _db.Carts
            join t in cartTotals on c.CartId equals t.CartId
            group t by c.CustomerId into g
            orderby g.Key
            select new CartTotalsByCustomerDto(g.Key, g.Sum(x => x.Total));

        return await q.AsNoTracking().ToListAsync(ct);
    }

    // ------------- ОКОННЫЕ ФУНКЦИИ (3 шт.) -------------
    // Для окон используем keyless DTO + FromSqlInterpolated (PostgreSQL).

    /// A) Ранк вариантов по цене внутри категории (DENSE_RANK по цене)
    public async Task<IReadOnlyList<PriceRankWithinCategoryDto>> PriceRankWithinCategoryAsync(
        string priceListCode, DateTimeOffset? at = null, CancellationToken ct = default)
    {
        var now = at ?? DateTimeOffset.UtcNow;

        return await _db.PriceRankWithinCategory
                .FromSqlInterpolated($$"""
                with current_prices as (
                    select pr.variant_id, pr.amount
                    from pricing.prices pr
                    join pricing.price_lists pl on pl.price_list_id = pr.price_list_id
                    where pl.code = {{priceListCode}}
                        and pr.valid_from <= {{now}}
                        and (pr.valid_to is null or {{now}} < pr.valid_to)
                )
                select 
                    c.category_id      as "CategoryId",
                    c.name             as "CategoryName",
                    p.product_id       as "ProductId",
                    p.name             as "ProductName",
                    v.variant_id       as "VariantId",
                    cp.amount          as "Price",
                    dense_rank() over (
                        partition by c.category_id
                        order by cp.amount
                    )                  as "PriceRankInCategory"
                from catalog.variants v
                join catalog.products p         on p.product_id = v.product_id
                left join catalog.categories c  on c.category_id = p.category_id
                join current_prices cp          on cp.variant_id = v.variant_id
                order by "CategoryName" nulls last, "PriceRankInCategory", "Price", "ProductName"
                """)
                .AsNoTracking()
                .ToListAsync(ct);
    }

    /// B) «Последняя» цена через ROW_NUMBER() (по (variant, price_list))
    public async Task<IReadOnlyList<LatestPriceDto>> LatestPricePerVariantAsync(
        string priceListCode, CancellationToken ct = default)
    {
        return await _db.LatestPrices
                .FromSqlInterpolated($$"""
                    select "VariantId", "PriceListCode", "Amount", "ValidFrom", "ValidTo"
                    from (
                        select 
                            pr.variant_id   as "VariantId",
                            pl.code         as "PriceListCode",
                            pr.amount       as "Amount",
                            pr.valid_from   as "ValidFrom",
                            pr.valid_to     as "ValidTo",
                            row_number() over (
                                partition by pr.variant_id, pl.code
                                order by pr.valid_from desc
                            ) as rn
                        from pricing.prices pr
                        join pricing.price_lists pl on pl.price_list_id = pr.price_list_id
                        where pl.code = {{priceListCode}}
                    ) t
                    where t.rn = 1
                """)
                .AsNoTracking()
                .ToListAsync(ct);
    }

    /// C) Накапливающаяся сумма стоимости корзин по времени для каждого клиента
    ///    (running total по UpdatedAt)
    public async Task<IReadOnlyList<RunningCartValueDto>> RunningCartValuePerCustomerAsync(CancellationToken ct = default)
    {
        return await _db.RunningCartValues
                .FromSqlInterpolated($$"""
                    with cart_totals as (
                        select 
                            c.cart_id                               as "CartId",
                            c.customer_id                           as "CustomerId",
                            c.updated_at                            as "UpdatedAt",
                            sum(ci.qty * ci.price_snapshot)::numeric(12,2) as "Total"
                        from sales.carts c
                        join sales.cart_items ci on ci.cart_id = c.cart_id
                        group by c.cart_id, c.customer_id, c.updated_at
                    )
                    select
                        "CustomerId",
                        "CartId",
                        "UpdatedAt",
                        "Total",
                        sum("Total") over (
                            partition by "CustomerId"
                            order by "UpdatedAt"
                            rows between unbounded preceding and current row
                        ) as "RunningTotalByCustomer"
                    from cart_totals
                    order by "CustomerId" nulls last, "UpdatedAt"
                """)
                .AsNoTracking()
                .ToListAsync(ct);

    }
}
