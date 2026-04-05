#!/bin/bash

GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m'

echo "🚀 Initializing ClickHouse for analytics project..."

if ! docker ps | grep -q clickhouse; then
    echo -e "${RED}❌ ClickHouse is not running. Please run: docker-compose up -d clickhouse${NC}"
    exit 1
fi

echo "🗑️  Dropping existing tables..."
docker exec -i clickhouse clickhouse-client < sql/05_drop_all.sql 2>/dev/null
echo -e "${GREEN}✓ Cleaned up old tables${NC}"

echo "📦 Creating database and tables..."

echo "  → Creating database and events table..."
docker exec -i clickhouse clickhouse-client < sql/01_create_database.sql

echo "  → Creating Kafka tables..."
docker exec -i clickhouse clickhouse-client < sql/02_create_table_for_topic.sql

echo "  → Creating materialized views..."
docker exec -i clickhouse clickhouse-client < sql/03_create_mv_topic_to_events.sql

echo "  → Creating aggregated views..."
docker exec -i clickhouse clickhouse-client < sql/04_create_aggregated.sql

echo -e "${GREEN}✓ All tables created successfully${NC}"

echo ""
echo "📋 Created tables:"
docker exec -it clickhouse clickhouse-client --query "SHOW TABLES FROM analytics"

echo ""
echo -e "${GREEN}✅ Initialization complete!${NC}"
echo ""
echo "Next steps:"
echo "  1. Run seed script: python3 seed.py"