#!/usr/bin/env python3
import random
import requests
from datetime import datetime, timedelta

CLICKHOUSE_URL = "http://localhost:8123/"

def insert_batch(events):
    if not events:
        return True
    
    lines = []
    for e in events:
        line = f'{{"event_time":"{e["event_time"]}","event_type":"{e["event_type"]}","user_id":{e["user_id"]},"product_id":{e["product_id"]},"shop_id":{e["shop_id"]},"related_product_id":{e["related_product_id"]},"kafka_topic":"{e["kafka_topic"]}","kafka_partition":{e["kafka_partition"]},"kafka_offset":{e["kafka_offset"]},"processed_at":"{e["processed_at"]}"}}'
        lines.append(line)
    
    data = "\n".join(lines)
    response = requests.post(CLICKHOUSE_URL + "?query=INSERT+INTO+analytics.events+FORMAT+JSONEachRow", data=data)
    return response.status_code == 200

USERS = list(range(1, 1001))
PRODUCTS = list(range(1, 501))
SHOPS = list(range(1, 51))

EVENT_TYPES = ['purchased', 'shown', 'viewed', 'liked', 'bought_together', 'visited']
EVENT_WEIGHTS = [0.10, 0.25, 0.35, 0.15, 0.05, 0.10]

def generate_event(event_time):
    event_type = random.choices(EVENT_TYPES, weights=EVENT_WEIGHTS)[0]
    
    event = {
        'event_time': event_time.strftime('%Y-%m-%d %H:%M:%S'),
        'event_type': event_type,
        'user_id': 0,
        'product_id': 0,
        'shop_id': 0,
        'related_product_id': 0,
        'kafka_topic': f'direct_{event_type}',
        'kafka_partition': 0,
        'kafka_offset': 0,
        'processed_at': datetime.now().strftime('%Y-%m-%d %H:%M:%S')
    }
    
    if event_type == 'purchased':
        event['user_id'] = random.choice(USERS)
        event['product_id'] = random.choice(PRODUCTS)
    elif event_type == 'shown':
        event['product_id'] = random.choice(PRODUCTS)
        event['user_id'] = random.choice(USERS)
    elif event_type == 'viewed':
        event['user_id'] = random.choice(USERS)
        event['product_id'] = random.choice(PRODUCTS)
    elif event_type == 'liked':
        event['user_id'] = random.choice(USERS)
        event['product_id'] = random.choice(PRODUCTS)
    elif event_type == 'bought_together':
        event['product_id'] = random.choice(PRODUCTS)
        event['related_product_id'] = random.choice(PRODUCTS)
        while event['related_product_id'] == event['product_id']:
            event['related_product_id'] = random.choice(PRODUCTS)
    elif event_type == 'visited':
        event['user_id'] = random.choice(USERS)
        event['shop_id'] = random.choice(SHOPS)
    
    return event

print("🚀 Generating data for LAST 10 DAYS (batch insert)")
print("=" * 50)

# Очищаем
requests.post(CLICKHOUSE_URL, data="TRUNCATE TABLE analytics.events")
print("✓ Cleared old data")

total = 0
today = datetime.now()
ten_days_ago = today - timedelta(days=10)

print(f"📅 Date range: {ten_days_ago.strftime('%Y-%m-%d')} to {today.strftime('%Y-%m-%d')}")
print()

batch = []
batch_size = 100000

dublicate_event = None

for day in range(60):
    current_date = ten_days_ago + timedelta(days=day)
    is_weekend = current_date.weekday() >= 5
    daily_events = 10000 if not is_weekend else 14500
    
    for _ in range(daily_events):
        hour = random.choices(range(24), weights=[0.5,0.5,0.5,0.5,0.5,1,2,3,4,5,6,5,4,3,2,2,3,4,5,6,5,3,2,1])[0]
        minute = random.randint(0, 59)
        second = random.randint(0, 59)
        event_time = datetime(current_date.year, current_date.month, current_date.day, hour, minute, second)
        event = generate_event(event_time)
        batch.append(event)
        if((hour == 15 or minute == 15 or second == 15) and dublicate_event != None):
            #print("add dublicate", dublicate_event)
            batch.append(dublicate_event)
            dublicate_event = None
        if(hour == 10 or minute == 10 or second == 10):
            #print("save dublicate", event)
            dublicate_event = event
        
        total += 1
        
        if len(batch) >= batch_size:
            insert_batch(batch)
            batch = []
            print(f"  → {total:,} events inserted...")
    
    print(f"  ✓ {current_date.strftime('%Y-%m-%d')}: {daily_events} events")

print(f"\n✅ DONE! Total events inserted: {total:,}")

r = requests.post(CLICKHOUSE_URL, data="SELECT count() FROM analytics.events")
print(f"✓ Verification: {r.text.strip()} events")

print("\n📊 Events by day:")
r = requests.post(CLICKHOUSE_URL, data="SELECT toDate(event_time) as date, count() FROM analytics.events GROUP BY date ORDER BY date")
print(r.text)