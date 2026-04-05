SELECT 
    hour,
    events_count,
    unique_users
FROM analytics.hourly_stats
WHERE date = today() - {0}
ORDER BY hour