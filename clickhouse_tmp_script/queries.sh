#!/bin/bash

if [ -z "$1" ]; then
    echo "Ошибка: не передан аргумент"
    echo "Использование: $0 <file>"
    exit 1
fi

docker exec -i clickhouse clickhouse-client < $1