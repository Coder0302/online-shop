-- Агрегированная витрина для ежечасной статистики
CREATE TABLE IF NOT EXISTS analytics.hourly_stats (
    date Date,
    hour UInt8,
    event_type String,
    events_count UInt64,
    unique_users UInt64,
    avg_duration Float64
) ENGINE = SummingMergeTree()
ORDER BY (date, hour, event_type);

-- Материализованное представление для заполнения витрины
CREATE MATERIALIZED VIEW IF NOT EXISTS analytics.hourly_stats_mv TO analytics.hourly_stats AS
SELECT 
    toDate(event_time) AS date,
    toHour(event_time) AS hour,
    event_type,
    count() AS events_count,
    uniq(user_id) AS unique_users,
    avg(duration_sec) AS avg_duration
FROM analytics.events
WHERE event_time IS NOT NULL
GROUP BY date, hour, event_type;

-- Витрина для анализа товаров по дням
CREATE TABLE IF NOT EXISTS analytics.daily_product_stats (
    date Date,
    product_id UInt64,
    views UInt64,
    likes UInt64,
    purchases UInt64,
    conversion_rate Float64
) ENGINE = SummingMergeTree()
ORDER BY (date, product_id);

-- Materialized View
CREATE MATERIALIZED VIEW IF NOT EXISTS analytics.daily_product_stats_mv
TO analytics.daily_product_stats
AS
SELECT 
    toDate(event_time) as date,
    product_id,
    countIf(event_type = 'viewed') as views,
    countIf(event_type = 'liked') as likes,
    countIf(event_type = 'purchased') as purchases,
    round(countIf(event_type = 'purchased') * 100.0 / 
          NULLIF(countIf(event_type = 'viewed'), 0), 2) as conversion_rate
FROM analytics.events
WHERE product_id > 0
GROUP BY date, product_id;