SELECT 
    product_id,
    views,
    purchases,
    round(purchases * 100.0 / views, 2) as conversion_rate
FROM (
    SELECT 
        product_id,
        countIf(event_type = 'viewed') as views,
        countIf(event_type = 'purchased') as purchases
    FROM analytics.events
    GROUP BY product_id
)
WHERE views > {0}
ORDER BY conversion_rate DESC
LIMIT {1};