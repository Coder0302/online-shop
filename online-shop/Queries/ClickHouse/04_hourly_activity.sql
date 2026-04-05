SELECT 
    toHour(event_time) as hour,
    event_type,
    count() as events_count,
    uniq(user_id) as unique_users
FROM analytics.events
WHERE event_time >= now() - INTERVAL {0} DAY
GROUP BY hour, event_type
ORDER BY hour, event_type;