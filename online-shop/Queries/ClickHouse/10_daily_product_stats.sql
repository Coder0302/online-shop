SELECT 
    product_id,
    views,
    likes,
    purchases,
    conversion_rate
FROM analytics.daily_product_stats
WHERE date >= (SELECT MAX(date) FROM analytics.daily_product_stats) - {0}
  AND views > {1}
ORDER BY conversion_rate DESC
LIMIT {2};