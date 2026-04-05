-- Создаем базу
CREATE DATABASE IF NOT EXISTS analytics;

-- Основная таблица
CREATE TABLE IF NOT EXISTS analytics.events (
    event_time DateTime64(3),
    
    event_type LowCardinality(String),
    
    user_id UInt64,
    product_id UInt64,
    shop_id UInt64,
    related_product_id UInt64,
    
    
    kafka_topic String,
    kafka_partition Int32,
    kafka_offset UInt64,
    processed_at DateTime64(3) DEFAULT now64()
) 
ENGINE = ReplacingMergeTree(processed_at)
PARTITION BY toYYYYMM(event_time)
ORDER BY (event_type, event_time, user_id, product_id)