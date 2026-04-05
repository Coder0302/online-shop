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
    round((events - (SELECT avg(events) FROM daily_stats)) * 100.0 / (SELECT avg(events) FROM daily_stats), 2) as deviation_pct
FROM daily_stats
ORDER BY deviation_pct DESC;