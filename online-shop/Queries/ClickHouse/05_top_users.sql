SELECT 
    user_id,
    count() as total_actions,
    uniq(product_id) as products_interacted,
    countIf(event_type = 'purchased') as purchases
FROM analytics.events
WHERE user_id > 0
GROUP BY user_id
ORDER BY total_actions DESC
LIMIT {0};