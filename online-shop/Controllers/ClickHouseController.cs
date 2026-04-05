using Microsoft.AspNetCore.Mvc;
using ECommerce.Models.ClickHouse;
using ECommerce.Services.ClickHouse;

namespace ECommerce.Controllers.ClickHouse;

/// <summary>
/// Системные эндпоинты для ClickHouse (дедубликация, диагностика)
/// </summary>
[Route("api/system/clickhouse")]
[ApiController]
public sealed class SystemClickHouseController : ControllerBase
{
    private readonly IClickHouseService _clickHouseService;

    public SystemClickHouseController(IClickHouseService clickHouseService)
    {
        _clickHouseService = clickHouseService;
    }

    /// <summary>
    /// Проверка дубликатов в таблице events
    /// </summary>
    [HttpGet("duplicates/check")]
    public async Task<ActionResult<string>> CheckDuplicates(
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        var result = await _clickHouseService.CheckDuplicatesAsync(limit, ct);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Принудительная дедубликация данных
    /// </summary>
    [HttpPost("duplicates/deduplicate")]
    public async Task<ActionResult<string>> Deduplicate(CancellationToken ct = default)
    {
        var result = await _clickHouseService.DeduplicateAsync(ct);
        return Ok(new { success = true, data = result });
    }
}

/// <summary>
/// Аналитические эндпоинты ClickHouse
/// </summary>
[Route("api/analytics/clickhouse")]
[ApiController]
public sealed class ClickHouseAnalyticsController : ControllerBase
{
    private readonly IClickHouseService _clickHouseService;
    private readonly ILogger<ClickHouseAnalyticsController> _logger;

    public ClickHouseAnalyticsController(IClickHouseService clickHouseService, ILogger<ClickHouseAnalyticsController> logger)
    {
        _clickHouseService = clickHouseService;
        _logger = logger;
    }

    /// <summary>
    /// Динамика продаж по дням
    /// </summary>
    [HttpGet("daily-sales")]
    public async Task<ActionResult<IReadOnlyList<DailySalesResponse>>> GetDailySales(
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        var result = await _clickHouseService.GetDailySalesAsync(limit, ct);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Топ товаров по выручке
    /// </summary>
    [HttpGet("top-products")]
    public async Task<ActionResult<IReadOnlyList<TopProductResponse>>> GetTopProducts(
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        var result = await _clickHouseService.GetTopProductsAsync(limit, ct);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Конверсия просмотр → покупка
    /// </summary>
    [HttpGet("conversion-rate")]
    public async Task<ActionResult<IReadOnlyList<ConversionRateResponse>>> GetConversionRate(
        [FromQuery] int minViews = 50,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        var result = await _clickHouseService.GetConversionRateAsync(minViews, limit, ct);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Активность по часам
    /// </summary>
    [HttpGet("hourly-activity")]
    public async Task<ActionResult<IReadOnlyList<HourlyActivityResponse>>> GetHourlyActivity(
        [FromQuery] int days = 7,
        CancellationToken ct = default)
    {
        var result = await _clickHouseService.GetHourlyActivityAsync(days, ct);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Топ пользователей по активности
    /// </summary>
    [HttpGet("top-users")]
    public async Task<ActionResult<IReadOnlyList<TopUserResponse>>> GetTopUsers(
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        var result = await _clickHouseService.GetTopUsersAsync(limit, ct);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Воронка событий
    /// </summary>
    [HttpGet("funnel")]
    public async Task<ActionResult<IReadOnlyList<FunnelResponse>>> GetFunnel(CancellationToken ct = default)
    {
        var result = await _clickHouseService.GetFunnelAnalysisAsync(ct);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Аномалии активности
    /// </summary>
    [HttpGet("anomalies")]
    public async Task<ActionResult<IReadOnlyList<AnomalyResponse>>> GetAnomalies(
        [FromQuery] int days = 14,
        CancellationToken ct = default)
    {
        var result = await _clickHouseService.GetAnomalyDetectionAsync(days, ct);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Retention пользователей
    /// </summary>
    [HttpGet("retention")]
    public async Task<ActionResult<IReadOnlyList<RetentionResponse>>> GetRetention(
        [FromQuery] int weeks = 4,
        CancellationToken ct = default)
    {
        var result = await _clickHouseService.GetUserRetentionAsync(weeks, ct);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Ежечасная статистика из витрины
    /// </summary>
    [HttpGet("hourly-stats")]
    public async Task<ActionResult<IReadOnlyList<HourlyStatsResponse>>> GetHourlyStats(
        [FromQuery] int daysBack = 1,
        CancellationToken ct = default)
    {
        var result = await _clickHouseService.GetHourlyStatsAsync(daysBack, ct);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Статистика по товарам из витрины
    /// </summary>
    [HttpGet("product-stats")]
    public async Task<ActionResult<IReadOnlyList<DailyProductStatsResponse>>> GetProductStats(
        [FromQuery] int daysBack = 1,
        [FromQuery] int minViews = 100,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        var result = await _clickHouseService.GetDailyProductStatsAsync(daysBack, minViews, limit, ct);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Сравнение производительности сырого запроса и витрины
    /// </summary>
    [HttpGet("performance-compare")]
    public async Task<ActionResult<PerformanceCompareResponse>> ComparePerformance(
        [FromQuery] int daysBack = 1,
        CancellationToken ct = default)
    {
        var result = await _clickHouseService.CompareRawVsViewAsync(daysBack, ct);
        return Ok(new { success = true, data = result });
    }
}