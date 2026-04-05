SELECT 
    'viewed' as stage,
    count(DISTINCT user_id) as users
FROM analytics.events WHERE event_type = 'viewed'
UNION ALL
SELECT 
    'liked',
    count(DISTINCT user_id)
FROM analytics.events WHERE event_type = 'liked'
UNION ALL
SELECT 
    'purchased',
    count(DISTINCT user_id)
FROM analytics.events WHERE event_type = 'purchased';