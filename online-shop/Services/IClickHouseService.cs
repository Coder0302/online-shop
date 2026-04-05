using ECommerce.Models.ClickHouse;

namespace ECommerce.Services.ClickHouse;

public interface IClickHouseService
{
    // Существующие методы...
    Task<IReadOnlyList<DailySalesResponse>> GetDailySalesAsync(int limit = 10, CancellationToken ct = default);
    Task<IReadOnlyList<TopProductResponse>> GetTopProductsAsync(int limit = 10, CancellationToken ct = default);
    Task<IReadOnlyList<ConversionRateResponse>> GetConversionRateAsync(int minViews = 50, int limit = 10, CancellationToken ct = default);
    Task<IReadOnlyList<HourlyActivityResponse>> GetHourlyActivityAsync(int days = 7, CancellationToken ct = default);
    Task<IReadOnlyList<TopUserResponse>> GetTopUsersAsync(int limit = 10, CancellationToken ct = default);
    Task<IReadOnlyList<FunnelResponse>> GetFunnelAnalysisAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AnomalyResponse>> GetAnomalyDetectionAsync(int days = 14, CancellationToken ct = default);
    Task<IReadOnlyList<RetentionResponse>> GetUserRetentionAsync(int weeks = 4, CancellationToken ct = default);
    Task<IReadOnlyList<HourlyStatsResponse>> GetHourlyStatsAsync(int daysBack = 1, CancellationToken ct = default);
    Task<IReadOnlyList<DailyProductStatsResponse>> GetDailyProductStatsAsync(int daysBack = 1, int minViews = 100, int limit = 10, CancellationToken ct = default);
    Task<PerformanceCompareResponse> CompareRawVsViewAsync(int daysBack = 1, CancellationToken ct = default);
    
    Task<string> CheckDuplicatesAsync(int limit = 10, CancellationToken ct = default);
    Task<string> DeduplicateAsync(CancellationToken ct = default);
}