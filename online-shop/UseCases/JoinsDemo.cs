using Microsoft.EntityFrameworkCore;

namespace ECommerce.App.UseCases;
public static class JoinsDemo
{
    public static async Task RunAsync(ECommerce.Data.ECommerceDbContext db, CancellationToken ct = default)
    {

        // (2 �������) ����������� 2 ������
        var q2_1 = await db.Products
            .Join(db.Brands, p => p.BrandId, b => b.BrandId, (p, b) => new { p.ProductId, p.Name, Brand = b.Name })
            .Take(100).ToListAsync(ct);

        var q2_2 = await db.Carts
            .Join(db.Customers, c => c.CustomerId, cu => cu.CustomerId, (c, cu) => new { c.CartId, cu.FirstName, cu.LastName })
            .Take(100).ToListAsync(ct);

        // (4 �������) ����������� 3 ������
        var q3_1 = await db.Variants
            .Join(db.Products, v => v.ProductId, p => p.ProductId, (v, p) => new { v, p })
            .Join(db.Categories, vp => vp.p.CategoryId, c => c.CategoryId, (vp, c) => new { vp.v.VariantId, Product = vp.p.Name, Category = c.Name })
            .Take(100).ToListAsync(ct);

        var q3_2 = await db.CartItems
            .Join(db.Carts, ci => ci.CartId, c => c.CartId, (ci, c) => new { ci, c })
            .Join(db.Customers, x => x.c.CustomerId, cu => cu.CustomerId, (x, cu) => new { x.ci, Customer = cu.FirstName })
            .Take(100).ToListAsync(ct);

        var q3_3 = await db.Prices
            .Join(db.PriceLists, p => p.PriceListId, pl => pl.PriceListId, (p, pl) => new { p, pl })
            .Join(db.Variants, x => x.p.VariantId, v => v.VariantId, (x, v) => new { x.p.Amount, x.pl.Code, v.Sku })
            .Take(100).ToListAsync(ct);

        var q3_4 = await db.ProductMedia
            .Join(db.Products, m => m.ProductId, p => p.ProductId, (m, p) => new { m, p })
            .Join(db.Brands, x => x.p.BrandId, b => b.BrandId, (x, b) => new { Media = x.m.Url, Product = x.p.Name, Brand = b.Name })
            .Take(100).ToListAsync(ct);

        // (1 ������) ����������� 4 ������
        var q4_1 = await db.CartItems
            .Join(db.Carts, ci => ci.CartId, c => c.CartId, (ci, c) => new { ci, c })
            .Join(db.Customers, x => x.c.CustomerId, cu => cu.CustomerId, (x, cu) => new { x.ci, cu })
            .Join(db.Variants, y => y.ci.VariantId, v => v.VariantId, (y, v) => new { y.ci, y.cu.FirstName, v.Sku })
            .Take(100).ToListAsync(ct);

        // (1 ������) ����������� 5 ������
        var q5_1 = await db.CartItems
            .Join(db.Carts, ci => ci.CartId, c => c.CartId, (ci, c) => new { ci, c })
            .Join(db.Customers, x => x.c.CustomerId, cu => cu.CustomerId, (x, cu) => new { x.ci, x.c, cu })
            .Join(db.Variants, y => y.ci.VariantId, v => v.VariantId, (y, v) => new { y.ci, y.c, y.cu, v })
            .Join(db.Products, z => z.v.ProductId, p => p.ProductId, (z, p) => new { z.ci, z.c, z.cu, z.v, p.Name })
            .Take(100).ToListAsync(ct);
    }
}
