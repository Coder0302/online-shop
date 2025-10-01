using Npgsql;

namespace ECommerce.Data.Seeding;
public static class BulkInserter
{
    public static async Task CopyCartsAsync(string connStr, IEnumerable<Entities.Sales.Cart> carts, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync(ct);
        await using var writer = await conn.BeginBinaryImportAsync(
            "COPY sales.carts (cart_id, customer_id, currency, created_at, updated_at) FROM STDIN BINARY", ct);

        foreach (var c in carts)
        {
            await writer.StartRowAsync(ct);
            writer.Write(c.CartId, NpgsqlTypes.NpgsqlDbType.Uuid);
            writer.Write(c.CustomerId, NpgsqlTypes.NpgsqlDbType.Uuid);
            writer.Write(c.Currency, NpgsqlTypes.NpgsqlDbType.Char);
            writer.Write(c.CreatedAt.UtcDateTime, NpgsqlTypes.NpgsqlDbType.TimestampTz);
            writer.Write(c.UpdatedAt.UtcDateTime, NpgsqlTypes.NpgsqlDbType.TimestampTz);
        }
        await writer.CompleteAsync(ct);
    }

    public static async Task CopyCartItemsToStagingAndUpsertAsync(
    string connStr,
    IEnumerable<Entities.Sales.CartItem> items,
    CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync(ct);

        await using var tx = await conn.BeginTransactionAsync(ct);

        // 1) временная (нежурналируемая) таблица для быстрой загрузки
        //    NB: UNLOGGED ускоряет COPY, TEMP автоматически удалится при коммите/ролбэке
        const string createStaging = @"
CREATE TEMP TABLE IF NOT EXISTS tmp_cart_items
(
  cart_item_id uuid NOT NULL,
  cart_id      uuid NOT NULL,
  variant_id   uuid NOT NULL,
  qty          integer NOT NULL,
  price_snapshot numeric(12,2) NOT NULL
) ON COMMIT DROP;";
        await using (var cmd = new NpgsqlCommand(createStaging, conn, tx))
            await cmd.ExecuteNonQueryAsync(ct);

        // 2) COPY в staging
        await using (var writer = await conn.BeginBinaryImportAsync(
            "COPY tmp_cart_items (cart_item_id, cart_id, variant_id, qty, price_snapshot) FROM STDIN BINARY", ct))
        {
            foreach (var it in items)
            {
                await writer.StartRowAsync(ct);
                writer.Write(it.CartItemId, NpgsqlTypes.NpgsqlDbType.Uuid);
                writer.Write(it.CartId, NpgsqlTypes.NpgsqlDbType.Uuid);
                writer.Write(it.VariantId, NpgsqlTypes.NpgsqlDbType.Uuid);
                writer.Write(it.Qty, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write(it.PriceSnapshot, NpgsqlTypes.NpgsqlDbType.Numeric);
            }
            await writer.CompleteAsync(ct);
        }

        // 3) UPSERT в целевую таблицу: суммируем qty при дублях по (cart_id, variant_id)
        const string upsert = @"
-- один оператор, без повторных конфликтов
INSERT INTO sales.cart_items (cart_item_id, cart_id, variant_id, qty, price_snapshot)
SELECT
  gen_random_uuid()        AS cart_item_id,       -- PK, не входит в уникальный ключ (cart_id, variant_id)
  t.cart_id,
  t.variant_id,
  SUM(t.qty)               AS qty,                -- схлопываем количество
  MIN(t.price_snapshot)    AS price_snapshot      -- пусть будет 0, потом массово обновим
FROM tmp_cart_items t
GROUP BY t.cart_id, t.variant_id
ON CONFLICT (cart_id, variant_id) DO UPDATE
SET qty = sales.cart_items.qty + EXCLUDED.qty;    -- увеличиваем qty при дубле
;";
        await using (var cmd2 = new NpgsqlCommand(upsert, conn, tx))
            await cmd2.ExecuteNonQueryAsync(ct);

        await tx.CommitAsync(ct);
    }

}
