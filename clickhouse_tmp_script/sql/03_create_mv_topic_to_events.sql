-- Materialized View для purchased
CREATE MATERIALIZED VIEW IF NOT EXISTS analytics.mv_purchased_to_events TO analytics.events AS
SELECT 
    parseDateTime64BestEffort(substring(time_str, 1, 19)) AS event_time,
    'purchased' AS event_type,
    id1 AS user_id,
    id2 AS product_id,
    0 AS shop_id,
    0 AS related_product_id,
    'purchased' AS kafka_topic,
    0 AS kafka_partition,
    0 AS kafka_offset,
    now64() AS processed_at
FROM analytics.kafka_purchased
WHERE time_str != '';

-- Materialized View для shown
CREATE MATERIALIZED VIEW IF NOT EXISTS analytics.mv_shown_to_events TO analytics.events AS
SELECT 
    parseDateTime64BestEffort(substring(time_str, 1, 19)) AS event_time,
    'shown' AS event_type,
    id2 AS user_id,
    id1 AS product_id,
    0 AS shop_id,
    0 AS related_product_id,
    'shown' AS kafka_topic,
    0 AS kafka_partition,
    0 AS kafka_offset,
    now64() AS processed_at
FROM analytics.kafka_shown
WHERE time_str != '';

-- Materialized View для viewed
CREATE MATERIALIZED VIEW IF NOT EXISTS analytics.mv_viewed_to_events TO analytics.events AS
SELECT 
    parseDateTime64BestEffort(substring(time_str, 1, 19)) AS event_time,
    'viewed' AS event_type,
    id1 AS user_id,
    id2 AS product_id,
    0 AS shop_id,
    0 AS related_product_id,
    'viewed' AS kafka_topic,
    0 AS kafka_partition,
    0 AS kafka_offset,
    now64() AS processed_at
FROM analytics.kafka_viewed
WHERE time_str != '';

-- Materialized View для liked
CREATE MATERIALIZED VIEW IF NOT EXISTS analytics.mv_liked_to_events TO analytics.events AS
SELECT 
    parseDateTime64BestEffort(substring(time_str, 1, 19)) AS event_time,
    'liked' AS event_type,
    id1 AS user_id,
    id2 AS product_id,
    0 AS shop_id,
    0 AS related_product_id,
    'liked' AS kafka_topic,
    0 AS kafka_partition,
    0 AS kafka_offset,
    now64() AS processed_at
FROM analytics.kafka_liked
WHERE time_str != '';

-- Materialized View для bought_together
CREATE MATERIALIZED VIEW IF NOT EXISTS analytics.mv_bought_together_to_events TO analytics.events AS
SELECT 
    parseDateTime64BestEffort(substring(time_str, 1, 19)) AS event_time,
    'bought_together' AS event_type,
    0 AS user_id,
    id1 AS product_id,
    0 AS shop_id,
    id2 AS related_product_id,
    'bought_together' AS kafka_topic,
    0 AS kafka_partition,
    0 AS kafka_offset,
    now64() AS processed_at
FROM analytics.kafka_bought_together
WHERE time_str != '';

-- Materialized View для visited
CREATE MATERIALIZED VIEW IF NOT EXISTS analytics.mv_visited_to_events TO analytics.events AS
SELECT 
    parseDateTime64BestEffort(substring(time_str, 1, 19)) AS event_time,
    'visited' AS event_type,
    id1 AS user_id,
    0 AS product_id,
    id2 AS shop_id,
    0 AS related_product_id,
    'visited' AS kafka_topic,
    0 AS kafka_partition,
    0 AS kafka_offset,
    now64() AS processed_at
FROM analytics.kafka_visited
WHERE time_str != '';