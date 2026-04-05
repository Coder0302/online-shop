SELECT 
    product_id,
    count() as purchase_count
FROM analytics.events 
WHERE event_type = 'purchased' AND product_id > 0
GROUP BY product_id
ORDER BY purchase_count DESC
LIMIT {0};