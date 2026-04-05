#!/bin/bash

echo "Running analytics queries..."

echo "--- Query 1: Events by type ---"
docker exec -i clickhouse clickhouse-client --query "
SELECT event_type, count() as events 
FROM analytics.events 
GROUP BY event_type 
ORDER BY events DESC"

echo ""
echo "--- Query 2: Events by day (with spikes) ---"
docker exec -i clickhouse clickhouse-client --query "
SELECT toDate(event_time) as date, count() as events 
FROM analytics.events 
GROUP BY date 
ORDER BY date DESC 
LIMIT 10"

echo ""
echo "--- Query 3: Top 5 products by purchases ---"
docker exec -i clickhouse clickhouse-client --query "
SELECT product_id, count() as purchases 
FROM analytics.events 
WHERE event_type='purchased' AND product_id > 0
GROUP BY product_id 
ORDER BY purchases DESC 
LIMIT 5"

echo ""
echo "--- Query 4: Top 5 users by activity ---"
docker exec -i clickhouse clickhouse-client --query "
SELECT user_id, count() as actions 
FROM analytics.events 
WHERE user_id > 0
GROUP BY user_id 
ORDER BY actions DESC 
LIMIT 5"

echo ""
echo "--- Query 5: Hourly activity on spike day (2024-01-08) ---"
docker exec -i clickhouse clickhouse-client --query "
SELECT toHour(event_time) as hour, count() as events 
FROM analytics.events 
WHERE toDate(event_time) = '2024-01-08'
GROUP BY hour 
ORDER BY hour"