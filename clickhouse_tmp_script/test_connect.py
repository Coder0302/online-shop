#!/usr/bin/env python3
import socket
import requests
import urllib.request
import json

print("=" * 60)
print("Testing ClickHouse connection")
print("=" * 60)

# Способ 1: Проверка TCP сокета на порт 9000
print("\n1. Testing TCP socket on port 9000...")
try:
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.settimeout(3)
    result = sock.connect_ex(('localhost', 9000))
    if result == 0:
        print("   ✓ Port 9000 is OPEN")
    else:
        print(f"   ✗ Port 9000 is CLOSED (error: {result})")
    sock.close()
except Exception as e:
    print(f"   ✗ Error: {e}")

# Способ 2: Проверка TCP сокета на порт 8123
print("\n2. Testing TCP socket on port 8123...")
try:
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.settimeout(3)
    result = sock.connect_ex(('localhost', 8123))
    if result == 0:
        print("   ✓ Port 8123 is OPEN")
    else:
        print(f"   ✗ Port 8123 is CLOSED (error: {result})")
    sock.close()
except Exception as e:
    print(f"   ✗ Error: {e}")

# Способ 3: HTTP GET запрос через urllib (порт 8123)
print("\n3. Testing HTTP GET via urllib (port 8123)...")
try:
    response = urllib.request.urlopen('http://localhost:8123/?query=SELECT+1', timeout=5)
    data = response.read().decode('utf-8')
    print(f"   ✓ Response: {data}")
except Exception as e:
    print(f"   ✗ Error: {e}")

# Способ 4: HTTP GET через requests (порт 8123)
print("\n4. Testing HTTP GET via requests (port 8123)...")
try:
    response = requests.get('http://localhost:8123/', params={'query': 'SELECT 1'}, timeout=5)
    print(f"   ✓ Status: {response.status_code}")
    print(f"   ✓ Response: {response.text.strip()}")
except Exception as e:
    print(f"   ✗ Error: {e}")

# Способ 5: HTTP POST через requests (порт 8123)
print("\n5. Testing HTTP POST via requests (port 8123)...")
try:
    response = requests.post('http://localhost:8123/', data='SELECT 1', timeout=5)
    print(f"   ✓ Status: {response.status_code}")
    print(f"   ✓ Response: {response.text.strip()}")
except Exception as e:
    print(f"   ✗ Error: {e}")

# Способ 6: clickhouse-driver (нативный протокол, порт 9000)
print("\n6. Testing clickhouse-driver (native protocol, port 9000)...")
try:
    from clickhouse_driver import Client
    client = Client(host='localhost', port=9000, user='default', password='')
    result = client.execute("SELECT 1")
    print(f"   ✓ Connected! Result: {result}")
except Exception as e:
    print(f"   ✗ Error: {e}")

print("\n" + "=" * 60)
print("Done")