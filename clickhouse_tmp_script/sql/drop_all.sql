-- Удаляем все таблицы в правильном порядке
DROP TABLE IF EXISTS analytics.hourly_stats_mv;
DROP TABLE IF EXISTS analytics.hourly_stats;
DROP TABLE IF EXISTS analytics.mv_purchase_to_events;
DROP TABLE IF EXISTS analytics.mv_show_to_events;
DROP TABLE IF EXISTS analytics.mv_view_to_events;
DROP TABLE IF EXISTS analytics.mv_like_to_events;
DROP TABLE IF EXISTS analytics.mv_sold_together_to_events;
DROP TABLE IF EXISTS analytics.mv_visit_to_events;
DROP TABLE IF EXISTS analytics.kafka_purchase;
DROP TABLE IF EXISTS analytics.kafka_show;
DROP TABLE IF EXISTS analytics.kafka_view;
DROP TABLE IF EXISTS analytics.kafka_like;
DROP TABLE IF EXISTS analytics.kafka_sold_together;
DROP TABLE IF EXISTS analytics.kafka_visit;
DROP TABLE IF EXISTS analytics.events;
DROP DATABASE IF EXISTS analytics;