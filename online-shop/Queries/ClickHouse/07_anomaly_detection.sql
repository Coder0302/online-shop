WITH daily_stats AS (
    SELECT 
        toDate(event_time) as date,
        count() as events
    FROM analytics.events
    WHERE toDate(event_time) >= now() - INTERVAL 14 DAY
    GROUP BY date
)
SELECT 
    date,
    events,
    round((events - avg_daily) * 100.0 / avg_daily, 2) as deviation_pct
FROM daily_stats,
    (SELECT avg(events) as avg_daily FROM daily_stats) as avg_table
ORDER BY deviation_pct DESC;