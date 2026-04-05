SELECT 
    toHour(event_time) as hour,
    count() as events_count,
    uniq(user_id) as unique_users
FROM analytics.events
WHERE toDate(event_time) = today() - {0}
GROUP BY hour
ORDER BY hour