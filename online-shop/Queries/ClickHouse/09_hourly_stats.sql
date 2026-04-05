-- Временный запрос без параметров
SELECT 
    hour,
    event_type,
    events_count,
    unique_users
FROM analytics.hourly_stats
WHERE date = '2026-05-24'
ORDER BY hour, event_type;