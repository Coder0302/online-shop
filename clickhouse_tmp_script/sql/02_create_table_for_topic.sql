-- Топик purchased (user → product)
CREATE TABLE IF NOT EXISTS analytics.kafka_purchased (
    id1 UInt64,
    id2 UInt64,
    time_str String
) ENGINE = Kafka
SETTINGS kafka_broker_list = 'kafka_f:9093',
         kafka_topic_list = 'purchased',
         kafka_group_name = 'clickhouse_consumer_group',
         kafka_format = 'JSONEachRow',
         kafka_num_consumers = 1;

-- Топик shown (product → user)
CREATE TABLE IF NOT EXISTS analytics.kafka_shown (
    id1 UInt64,
    id2 UInt64,
    time_str String
) ENGINE = Kafka
SETTINGS kafka_broker_list = 'kafka_f:9093',
         kafka_topic_list = 'shown',
         kafka_group_name = 'clickhouse_consumer_group',
         kafka_format = 'JSONEachRow';

-- Топик viewed (user → product)
CREATE TABLE IF NOT EXISTS analytics.kafka_viewed (
    id1 UInt64,
    id2 UInt64,
    time_str String
) ENGINE = Kafka
SETTINGS kafka_broker_list = 'kafka_f:9093',
         kafka_topic_list = 'viewed',
         kafka_group_name = 'clickhouse_consumer_group',
         kafka_format = 'JSONEachRow';

-- Топик liked (user → product)
CREATE TABLE IF NOT EXISTS analytics.kafka_liked (
    id1 UInt64,
    id2 UInt64,
    time_str String
) ENGINE = Kafka
SETTINGS kafka_broker_list = 'kafka_f:9093',
         kafka_topic_list = 'liked',
         kafka_group_name = 'clickhouse_consumer_group',
         kafka_format = 'JSONEachRow';

-- Топик bought_together (product ↔ product)
CREATE TABLE IF NOT EXISTS analytics.kafka_bought_together (
    id1 UInt64,
    id2 UInt64,
    time_str String
) ENGINE = Kafka
SETTINGS kafka_broker_list = 'kafka_f:9093',
         kafka_topic_list = 'bought_together',
         kafka_group_name = 'clickhouse_consumer_group',
         kafka_format = 'JSONEachRow';

-- Топик visited (user → shop)
CREATE TABLE IF NOT EXISTS analytics.kafka_visited (
    id1 UInt64,
    id2 UInt64,
    time_str String
) ENGINE = Kafka
SETTINGS kafka_broker_list = 'kafka_f:9093',
         kafka_topic_list = 'visited',
         kafka_group_name = 'clickhouse_consumer_group',
         kafka_format = 'JSONEachRow';