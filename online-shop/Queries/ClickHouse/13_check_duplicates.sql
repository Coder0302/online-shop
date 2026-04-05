SELECT 
    event_type,
    event_time,
    user_id,
    product_id,
    count() as duplicates,
    groupArray(processed_at) as timestamps
FROM analytics.events
GROUP BY event_type, event_time, user_id, product_id
HAVING count() > 1
LIMIT {0}