#!/usr/bin/env python3
import requests

# Пробуем вставить одну запись через HTTP
query = """
INSERT INTO analytics.events (event_time, event_type, user_id, product_id, shop_id, related_product_id, kafka_topic, kafka_partition, kafka_offset, processed_at)
VALUES ('2024-01-01 12:00:00', 'purchased', 1, 100, 0, 0, 'test', 0, 0, '2024-01-01 12:00:00')
"""

response = requests.post('http://localhost:8123/', data=query)
print(f"Status: {response.status_code}")
print(f"Response: {response.text}")

# Проверяем
check = requests.get('http://localhost:8123/', params={'query': 'SELECT count() FROM analytics.events'})
print(f"Count: {check.text}")