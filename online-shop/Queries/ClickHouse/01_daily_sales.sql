SELECT 
    toDate(event_time) as sale_date,
    count() as total_sales
FROM analytics.events 
WHERE event_type = 'purchased'
GROUP BY sale_date
ORDER BY sale_date DESC
LIMIT {0};