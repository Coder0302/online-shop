SELECT 
    toWeek(event_time) as week,
    count(DISTINCT user_id) as active_users,
    round(
        count(DISTINCT user_id) * 100.0 / 
        NULLIF(LAG(count(DISTINCT user_id)) OVER (ORDER BY toWeek(event_time)), 0), 
        2
    ) as retention_rate
FROM analytics.events
WHERE toWeek(event_time) >= toWeek(now()) - 4
GROUP BY week
ORDER BY week DESC;