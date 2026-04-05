namespace ECommerce.Models.ClickHouse;

public record DailySalesResponse(
    DateTime SaleDate,
    long TotalSales);

public record TopProductResponse(
    ulong ProductId,
    long PurchaseCount);

public record ConversionRateResponse(
    ulong ProductId,
    long Views,
    long Purchases,
    decimal ConversionRate);

public record HourlyActivityResponse(
    int Hour,
    string EventType,
    long EventsCount,
    long UniqueUsers);

public record TopUserResponse(
    ulong UserId,
    long TotalActions,
    long ProductsInteracted,
    long Purchases);

public record FunnelResponse(
    string Stage,
    long Users);

public record AnomalyResponse(
    DateTime Date,
    long Events,
    decimal DeviationPercent);

public record RetentionResponse(
    int Week,
    long ActiveUsers,
    decimal RetentionRate);

public record HourlyStatsResponse(
    int Hour,
    string EventType,
    long EventsCount,
    long UniqueUsers);

public record DailyProductStatsResponse(
    ulong ProductId,
    long Views,
    long Likes,
    long Purchases,
    decimal ConversionRate);

public record PerformanceCompareResponse(
    string QueryType,
    long RawExecutionTimeMs,
    long ViewExecutionTimeMs,
    int RawRowsReturned = 0,
    int ViewRowsReturned = 0);