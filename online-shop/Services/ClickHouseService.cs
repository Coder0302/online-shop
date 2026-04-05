using System.Text;
using System.Diagnostics;
using ClickHouse.Client.ADO;
using ECommerce.Models.ClickHouse;

namespace ECommerce.Services.ClickHouse;

public sealed class ClickHouseService : IClickHouseService, IDisposable
{
    private readonly ClickHouseConnection _connection;
    private readonly ILogger<ClickHouseService> _logger;
    private readonly string _queriesPath;

    public ClickHouseService(IConfiguration configuration, ILogger<ClickHouseService> logger)
    {
        var connectionString = configuration.GetConnectionString("ClickHouse") 
            ?? "Host=localhost;Port=8123;User=default;Password=;Database=analytics";
        
        _connection = new ClickHouseConnection(connectionString);
        _logger = logger;
        var projectRoot = Directory.GetCurrentDirectory();
        _queriesPath = Path.Combine(projectRoot, "Queries", "ClickHouse");
    }

    public async Task<IReadOnlyList<DailySalesResponse>> GetDailySalesAsync(int limit = 10, CancellationToken ct = default)
    {
        var sql = await File.ReadAllTextAsync(Path.Combine(_queriesPath, "01_daily_sales.sql"), ct);
        sql = string.Format(sql, limit);
        
        await _connection.OpenAsync(ct);
        
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        
        using var reader = await command.ExecuteReaderAsync(ct);
        var results = new List<DailySalesResponse>();
        
        while (await reader.ReadAsync(ct))
        {
            results.Add(new DailySalesResponse(
                reader.GetDateTime(0),
                Convert.ToInt64(reader.GetFieldValue<ulong>(1))));
        }
        
        return results;
    }

    public async Task<IReadOnlyList<TopProductResponse>> GetTopProductsAsync(int limit = 10, CancellationToken ct = default)
    {
        var sql = await File.ReadAllTextAsync(Path.Combine(_queriesPath, "02_top_products.sql"), ct);
        sql = string.Format(sql, limit);
        
        await _connection.OpenAsync(ct);
        
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        
        using var reader = await command.ExecuteReaderAsync(ct);
        var results = new List<TopProductResponse>();
        
        while (await reader.ReadAsync(ct))
        {
            results.Add(new TopProductResponse(
                reader.GetFieldValue<ulong>(0),
                Convert.ToInt64(reader.GetFieldValue<ulong>(1))));
        }
        
        return results;
    }

    public async Task<IReadOnlyList<ConversionRateResponse>> GetConversionRateAsync(int minViews = 50, int limit = 10, CancellationToken ct = default)
    {
        var sql = await File.ReadAllTextAsync(Path.Combine(_queriesPath, "03_conversion_rate.sql"), ct);
        sql = string.Format(sql, minViews, limit);
        
        await _connection.OpenAsync(ct);
        
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        
        using var reader = await command.ExecuteReaderAsync(ct);
        var results = new List<ConversionRateResponse>();
        
        while (await reader.ReadAsync(ct))
        {
            results.Add(new ConversionRateResponse(
                reader.GetFieldValue<ulong>(0),
                Convert.ToInt64(reader.GetFieldValue<ulong>(1)),
                Convert.ToInt64(reader.GetFieldValue<ulong>(2)),
                Convert.ToDecimal(reader.GetDouble(3))));
        }
        
        return results;
    }

    public async Task<IReadOnlyList<HourlyActivityResponse>> GetHourlyActivityAsync(int days = 7, CancellationToken ct = default)
    {
        var sql = await File.ReadAllTextAsync(Path.Combine(_queriesPath, "04_hourly_activity.sql"), ct);
        sql = string.Format(sql, days);
        
        await _connection.OpenAsync(ct);
        
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        
        using var reader = await command.ExecuteReaderAsync(ct);
        var results = new List<HourlyActivityResponse>();
        
        while (await reader.ReadAsync(ct))
        {
            results.Add(new HourlyActivityResponse(
                reader.GetInt32(0),
                reader.GetString(1),
                Convert.ToInt64(reader.GetFieldValue<ulong>(2)),
                Convert.ToInt64(reader.GetFieldValue<ulong>(3))));
        }
        
        return results;
    }

    public async Task<IReadOnlyList<TopUserResponse>> GetTopUsersAsync(int limit = 10, CancellationToken ct = default)
    {
        var sql = await File.ReadAllTextAsync(Path.Combine(_queriesPath, "05_top_users.sql"), ct);
        sql = string.Format(sql, limit);
        
        await _connection.OpenAsync(ct);
        
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        
        using var reader = await command.ExecuteReaderAsync(ct);
        var results = new List<TopUserResponse>();
        
        while (await reader.ReadAsync(ct))
        {
            results.Add(new TopUserResponse(
                reader.GetFieldValue<ulong>(0),
                Convert.ToInt64(reader.GetFieldValue<ulong>(1)),
                Convert.ToInt64(reader.GetFieldValue<ulong>(2)),
                Convert.ToInt64(reader.GetFieldValue<ulong>(3))));
        }
        
        return results;
    }

    public async Task<IReadOnlyList<FunnelResponse>> GetFunnelAnalysisAsync(CancellationToken ct = default)
    {
        var sql = await File.ReadAllTextAsync(Path.Combine(_queriesPath, "06_funnel_analysis.sql"), ct);
        
        await _connection.OpenAsync(ct);
        
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        
        using var reader = await command.ExecuteReaderAsync(ct);
        var results = new List<FunnelResponse>();
        
        while (await reader.ReadAsync(ct))
        {
            results.Add(new FunnelResponse(
                reader.GetString(0),
                Convert.ToInt64(reader.GetFieldValue<ulong>(1))));
        }
        
        return results;
    }

    public async Task<IReadOnlyList<AnomalyResponse>> GetAnomalyDetectionAsync(int days = 14, CancellationToken ct = default)
    {
        var sql = await File.ReadAllTextAsync(Path.Combine(_queriesPath, "07_anomaly_detection.sql"), ct);
        sql = string.Format(sql, days);
        
        await _connection.OpenAsync(ct);
        
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        
        using var reader = await command.ExecuteReaderAsync(ct);
        var results = new List<AnomalyResponse>();
        
        while (await reader.ReadAsync(ct))
        {
            results.Add(new AnomalyResponse(
                reader.GetDateTime(0),
                Convert.ToInt64(reader.GetFieldValue<ulong>(1)),
                Convert.ToDecimal(reader.GetDouble(2))));
        }
        
        return results;
    }

    public async Task<IReadOnlyList<RetentionResponse>> GetUserRetentionAsync(int weeks = 4, CancellationToken ct = default)
    {
        var sql = await File.ReadAllTextAsync(Path.Combine(_queriesPath, "08_user_retention.sql"), ct);
        
        await _connection.OpenAsync(ct);
        
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        
        using var reader = await command.ExecuteReaderAsync(ct);
        var results = new List<RetentionResponse>();
        
        while (await reader.ReadAsync(ct))
        {
            int week = Convert.ToInt32(reader.GetFieldValue<byte>(0));
            long activeUsers = Convert.ToInt64(reader.GetFieldValue<ulong>(1));
            
            decimal retentionRate = 0;
            if (!reader.IsDBNull(2))
            {
                retentionRate = Convert.ToDecimal(reader.GetDouble(2));
            }
            
            results.Add(new RetentionResponse(week, activeUsers, retentionRate));
        }
        
        return results;
    }

    public async Task<IReadOnlyList<HourlyStatsResponse>> GetHourlyStatsAsync(int daysBack = 1, CancellationToken ct = default)
    {
        var sql = await File.ReadAllTextAsync(Path.Combine(_queriesPath, "09_hourly_stats.sql"), ct);
        sql = string.Format(sql, daysBack);
        
        await _connection.OpenAsync(ct);
        
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        
        using var reader = await command.ExecuteReaderAsync(ct);
        var results = new List<HourlyStatsResponse>();
        
        while (await reader.ReadAsync(ct))
        {
            results.Add(new HourlyStatsResponse(
                Convert.ToInt32(reader.GetFieldValue<byte>(0)),
                reader.GetString(1),
                Convert.ToInt64(reader.GetFieldValue<ulong>(2)),
                Convert.ToInt64(reader.GetFieldValue<ulong>(3))));
        }
        
        return results;
    }

    public async Task<IReadOnlyList<DailyProductStatsResponse>> GetDailyProductStatsAsync(int daysBack = 1, int minViews = 100, int limit = 10, CancellationToken ct = default)
    {
        var sql = await File.ReadAllTextAsync(Path.Combine(_queriesPath, "10_daily_product_stats.sql"), ct);
        sql = string.Format(sql, daysBack, minViews, limit);
        
        await _connection.OpenAsync(ct);
        
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        
        using var reader = await command.ExecuteReaderAsync(ct);
        var results = new List<DailyProductStatsResponse>();
        
        while (await reader.ReadAsync(ct))
        {
            results.Add(new DailyProductStatsResponse(
                reader.GetFieldValue<ulong>(0),
                Convert.ToInt64(reader.GetFieldValue<ulong>(1)),
                Convert.ToInt64(reader.GetFieldValue<ulong>(2)),
                Convert.ToInt64(reader.GetFieldValue<ulong>(3)),
                Convert.ToDecimal(reader.GetDouble(4))));
        }
        
        return results;
    }

    public async Task<PerformanceCompareResponse> CompareRawVsViewAsync(int daysBack = 1, CancellationToken ct = default)
    {
        var rawSql = await File.ReadAllTextAsync(Path.Combine(_queriesPath, "11_performance_compare_raw.sql"), ct);
        rawSql = string.Format(rawSql, daysBack);
        
        var viewSql = await File.ReadAllTextAsync(Path.Combine(_queriesPath, "12_performance_compare_view.sql"), ct);
        viewSql = string.Format(viewSql, daysBack);
        
        var stopwatch = new Stopwatch();
        
        await _connection.OpenAsync(ct);
        
        // Raw query
        using var rawCommand = _connection.CreateCommand();
        rawCommand.CommandText = rawSql;
        
        stopwatch.Restart();
        using var rawReader = await rawCommand.ExecuteReaderAsync(ct);
        var rawTime = stopwatch.ElapsedMilliseconds;
        var rawRows = 0;
        while (await rawReader.ReadAsync(ct)) rawRows++;
        
        // View query
        using var viewCommand = _connection.CreateCommand();
        viewCommand.CommandText = viewSql;
        
        stopwatch.Restart();
        using var viewReader = await viewCommand.ExecuteReaderAsync(ct);
        var viewTime = stopwatch.ElapsedMilliseconds;
        var viewRows = 0;
        while (await viewReader.ReadAsync(ct)) viewRows++;
        
        return new PerformanceCompareResponse(
            $"Raw vs View (days back: {daysBack})",
            rawTime,
            viewTime,
            rawRows,
            viewRows);
    }

    public async Task<string> CheckDuplicatesAsync(int limit = 10, CancellationToken ct = default)
    {
        var sql = await File.ReadAllTextAsync(Path.Combine(_queriesPath, "13_check_duplicates.sql"), ct);
        sql = string.Format(sql, limit);
        
        await _connection.OpenAsync(ct);
        
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        
        using var reader = await command.ExecuteReaderAsync(ct);
        var results = new List<string>();
        
        while (await reader.ReadAsync(ct))
        {
            results.Add($"{reader.GetString(0)} | {reader.GetDateTime(1)} | user:{reader.GetFieldValue<ulong>(2)} | product:{reader.GetFieldValue<ulong>(3)} | dup:{reader.GetFieldValue<ulong>(4)}");
        }
        
        return results.Count == 0 ? "No duplicates found" : string.Join("\n", results);
    }

    public async Task<string> DeduplicateAsync(CancellationToken ct = default)
    {
        var sql = await File.ReadAllTextAsync(Path.Combine(_queriesPath, "14_deduplicate.sql"), ct);
        
        await _connection.OpenAsync(ct);
        
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        
        var stopwatch = Stopwatch.StartNew();
        await command.ExecuteNonQueryAsync(ct);
        stopwatch.Stop();
        
        return $"Deduplication completed in {stopwatch.ElapsedMilliseconds} ms";
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}