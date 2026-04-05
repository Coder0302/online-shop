ALTER TABLE analytics.events 
MODIFY TTL event_time + INTERVAL 90 DAY;

ALTER TABLE analytics.hourly_stats 
MODIFY TTL date + INTERVAL 3 YEAR;

ALTER TABLE analytics.daily_product_stats 
MODIFY TTL date + INTERVAL 3 YEAR;