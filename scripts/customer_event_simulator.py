#!/usr/bin/env python3
"""Generate shopper event pipelines and send them to the online-shop HTTP API."""

from __future__ import annotations

import argparse
import json
import random
import sys
import time
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
from typing import Any
from urllib import error, parse, request


DEFAULT_BASE_URL = "http://localhost:5000"


@dataclass(frozen=True)
class Product:
    product_id: str
    name: str
    tags: list[str]


@dataclass(frozen=True)
class Shopper:
    user_id: str
    name: str


@dataclass(frozen=True)
class ShopperEvent:
    event_type: str
    user_id: str
    product_id: str
    user_name: str
    product_name: str
    product_tags: list[str]
    occurred_at_utc: datetime
    rating: int | None = None

    def to_payload(self) -> dict[str, Any]:
        payload: dict[str, Any] = {
            "eventType": self.event_type,
            "userId": self.user_id,
            "productId": self.product_id,
            "userName": self.user_name,
            "productName": self.product_name,
            "productTags": self.product_tags,
            "occurredAtUtc": to_iso8601_utc(self.occurred_at_utc),
        }
        if self.rating is not None:
            payload["rating"] = self.rating
        return payload


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate customer pipelines like shown -> viewed -> liked -> purchased and POST them to the shop API."
    )
    parser.add_argument("--base-url", default=DEFAULT_BASE_URL, help="API base URL, for example http://localhost:5000")
    parser.add_argument("--users", type=int, default=250, help="Number of synthetic shoppers")
    parser.add_argument("--sessions", type=int, default=1200, help="Number of sessions to generate")
    parser.add_argument("--products-limit", type=int, default=200, help="How many products to fetch from catalog APIs")
    parser.add_argument("--batch-size", type=int, default=200, help="How many events to send in one POST")
    parser.add_argument("--sleep-ms", type=int, default=0, help="Pause between batch sends")
    parser.add_argument("--lookback-minutes", type=int, default=180, help="How far into the past to randomize timestamps")
    parser.add_argument("--min-products-per-session", type=int, default=1, help="Minimum products shown in one session")
    parser.add_argument("--max-products-per-session", type=int, default=5, help="Maximum products shown in one session")
    parser.add_argument("--min-step-seconds", type=int, default=2, help="Minimum gap between events in a session")
    parser.add_argument("--max-step-seconds", type=int, default=45, help="Maximum gap between events in a session")
    parser.add_argument("--view-probability", type=float, default=0.74, help="Probability of view after shown")
    parser.add_argument("--like-probability", type=float, default=0.28, help="Probability of like after viewed")
    parser.add_argument("--purchase-after-view-probability", type=float, default=0.10, help="Probability of purchase after viewed")
    parser.add_argument("--purchase-after-like-probability", type=float, default=0.34, help="Probability of purchase after liked")
    parser.add_argument("--seed", type=int, default=42, help="Random seed")
    parser.add_argument("--dry-run", action="store_true", help="Generate events without sending them")
    parser.add_argument("--timeout-seconds", type=int, default=30, help="HTTP timeout")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    validate_args(args)

    rng = random.Random(args.seed)
    base_url = args.base_url.rstrip("/")

    products = load_products(base_url, args.products_limit, args.timeout_seconds)
    if not products:
        print("No products were loaded from API. Stop.", file=sys.stderr)
        return 1

    shoppers = build_shoppers(args.users)
    batches = list(
        generate_batches(
            rng=rng,
            shoppers=shoppers,
            products=products,
            sessions=args.sessions,
            batch_size=args.batch_size,
            lookback_minutes=args.lookback_minutes,
            min_products_per_session=args.min_products_per_session,
            max_products_per_session=args.max_products_per_session,
            min_step_seconds=args.min_step_seconds,
            max_step_seconds=args.max_step_seconds,
            view_probability=args.view_probability,
            like_probability=args.like_probability,
            purchase_after_view_probability=args.purchase_after_view_probability,
            purchase_after_like_probability=args.purchase_after_like_probability,
        )
    )

    total_events = sum(len(batch) for batch in batches)
    counts = count_events(event for batch in batches for event in batch)

    print(f"Loaded products: {len(products)}")
    print(f"Prepared shoppers: {len(shoppers)}")
    print(f"Prepared sessions: {args.sessions}")
    print(f"Prepared events: {total_events}")
    print(f"Distribution: {json.dumps(counts, ensure_ascii=False)}")

    if args.dry_run:
        preview_events = [event.to_payload() for event in (batches[0][: min(5, len(batches[0]))] if batches else [])]
        print("Dry-run preview:")
        print(json.dumps(preview_events, ensure_ascii=False, indent=2))
        return 0

    sent_events = 0
    for batch_index, batch in enumerate(batches, start=1):
        payload = {
            "events": [event.to_payload() for event in batch],
            "continueOnError": False,
        }
        response = http_json(
            "POST",
            join_url(base_url, "/api/shop/events/batch"),
            timeout_seconds=args.timeout_seconds,
            payload=payload,
        )

        sent_events += len(batch)
        processed = response.get("processed", response.get("Processed"))
        failed = response.get("failed", response.get("Failed"))
        print(
            f"Batch {batch_index}/{len(batches)}: sent={len(batch)} processed={processed} failed={failed} total_sent={sent_events}"
        )

        if args.sleep_ms > 0 and batch_index != len(batches):
            time.sleep(args.sleep_ms / 1000.0)

    print("Simulation completed.")
    return 0


def validate_args(args: argparse.Namespace) -> None:
    if args.users <= 0:
        raise SystemExit("--users must be > 0")
    if args.sessions <= 0:
        raise SystemExit("--sessions must be > 0")
    if args.products_limit <= 0:
        raise SystemExit("--products-limit must be > 0")
    if args.batch_size <= 0:
        raise SystemExit("--batch-size must be > 0")
    if args.lookback_minutes <= 0:
        raise SystemExit("--lookback-minutes must be > 0")
    if args.min_products_per_session <= 0:
        raise SystemExit("--min-products-per-session must be > 0")
    if args.max_products_per_session < args.min_products_per_session:
        raise SystemExit("--max-products-per-session must be >= --min-products-per-session")
    if args.min_step_seconds <= 0:
        raise SystemExit("--min-step-seconds must be > 0")
    if args.max_step_seconds < args.min_step_seconds:
        raise SystemExit("--max-step-seconds must be >= --min-step-seconds")

    probability_args = {
        "--view-probability": args.view_probability,
        "--like-probability": args.like_probability,
        "--purchase-after-view-probability": args.purchase_after_view_probability,
        "--purchase-after-like-probability": args.purchase_after_like_probability,
    }
    for name, value in probability_args.items():
        if value < 0 or value > 1:
            raise SystemExit(f"{name} must be between 0 and 1")


def build_shoppers(count: int) -> list[Shopper]:
    return [
        Shopper(
            user_id=f"sim-user-{index:05d}",
            name=f"Sim User {index:05d}",
        )
        for index in range(1, count + 1)
    ]


def load_products(base_url: str, limit: int, timeout_seconds: int) -> list[Product]:
    loaders = [
        lambda: load_products_from_mongo(base_url, limit, timeout_seconds),
        lambda: load_products_from_postgres(base_url, limit, timeout_seconds),
    ]

    for loader in loaders:
        try:
            products = loader()
        except error.HTTPError as exc:
            print(f"Catalog load failed with HTTP {exc.code}: {exc.reason}", file=sys.stderr)
            continue
        except error.URLError as exc:
            print(f"Catalog load failed: {exc.reason}", file=sys.stderr)
            continue

        if products:
            return products

    return []


def load_products_from_mongo(base_url: str, limit: int, timeout_seconds: int) -> list[Product]:
    url = join_url(base_url, f"/api/mongo/products?take={limit}&isActive=true")
    data = http_json("GET", url, timeout_seconds=timeout_seconds)
    products: list[Product] = []
    for item in ensure_list(data):
        product_id = str(item.get("id", "")).strip()
        if not product_id:
            continue

        tags = compact_tags(item.get("category"))
        products.append(
            Product(
                product_id=product_id,
                name=str(item.get("name", product_id)).strip() or product_id,
                tags=tags,
            )
        )
    return products


def load_products_from_postgres(base_url: str, limit: int, timeout_seconds: int) -> list[Product]:
    url = join_url(base_url, f"/api/postgres/products/active?take={limit}")
    data = http_json("GET", url, timeout_seconds=timeout_seconds)
    products: list[Product] = []
    for item in ensure_list(data):
        product_id = str(item.get("productId", "")).strip()
        if not product_id:
            continue

        tags = compact_tags(item.get("categoryName"), item.get("brandName"))
        products.append(
            Product(
                product_id=product_id,
                name=str(item.get("name", product_id)).strip() or product_id,
                tags=tags,
            )
        )
    return products


def ensure_list(data: Any) -> list[dict[str, Any]]:
    if isinstance(data, list):
        return [item for item in data if isinstance(item, dict)]
    raise ValueError("Expected JSON array from product API")


def generate_batches(
    *,
    rng: random.Random,
    shoppers: list[Shopper],
    products: list[Product],
    sessions: int,
    batch_size: int,
    lookback_minutes: int,
    min_products_per_session: int,
    max_products_per_session: int,
    min_step_seconds: int,
    max_step_seconds: int,
    view_probability: float,
    like_probability: float,
    purchase_after_view_probability: float,
    purchase_after_like_probability: float,
) -> list[list[ShopperEvent]]:
    now = datetime.now(timezone.utc)
    buffer: list[ShopperEvent] = []
    batches: list[list[ShopperEvent]] = []

    for _ in range(sessions):
        shopper = rng.choice(shoppers)
        session_events = generate_session(
            rng=rng,
            shopper=shopper,
            products=products,
            now=now,
            lookback_minutes=lookback_minutes,
            min_products_per_session=min_products_per_session,
            max_products_per_session=max_products_per_session,
            min_step_seconds=min_step_seconds,
            max_step_seconds=max_step_seconds,
            view_probability=view_probability,
            like_probability=like_probability,
            purchase_after_view_probability=purchase_after_view_probability,
            purchase_after_like_probability=purchase_after_like_probability,
        )

        buffer.extend(session_events)
        while len(buffer) >= batch_size:
            batches.append(buffer[:batch_size])
            buffer = buffer[batch_size:]

    if buffer:
        batches.append(buffer)

    return batches


def generate_session(
    *,
    rng: random.Random,
    shopper: Shopper,
    products: list[Product],
    now: datetime,
    lookback_minutes: int,
    min_products_per_session: int,
    max_products_per_session: int,
    min_step_seconds: int,
    max_step_seconds: int,
    view_probability: float,
    like_probability: float,
    purchase_after_view_probability: float,
    purchase_after_like_probability: float,
) -> list[ShopperEvent]:
    session_start = now - timedelta(seconds=rng.randint(0, lookback_minutes * 60))
    cursor = session_start

    max_products = min(max_products_per_session, len(products))
    product_count = rng.randint(min_products_per_session, max_products)
    session_products = rng.sample(products, product_count)

    events: list[ShopperEvent] = []
    for product in session_products:
        cursor = advance_cursor(rng, cursor, min_step_seconds, max_step_seconds)
        events.append(build_event("shown", shopper, product, cursor))

        if rng.random() > view_probability:
            continue

        cursor = advance_cursor(rng, cursor, min_step_seconds, max_step_seconds)
        events.append(build_event("viewed", shopper, product, cursor))

        liked = False
        if rng.random() <= like_probability:
            cursor = advance_cursor(rng, cursor, min_step_seconds, max_step_seconds)
            events.append(build_event("liked", shopper, product, cursor))
            liked = True

        purchase_probability = (
            purchase_after_like_probability if liked else purchase_after_view_probability
        )
        if rng.random() <= purchase_probability:
            cursor = advance_cursor(rng, cursor, min_step_seconds, max_step_seconds * 2)
            events.append(build_event("purchased", shopper, product, cursor, rating=random_rating(rng)))

    return events


def build_event(
    event_type: str,
    shopper: Shopper,
    product: Product,
    occurred_at_utc: datetime,
    rating: int | None = None,
) -> ShopperEvent:
    return ShopperEvent(
        event_type=event_type,
        user_id=shopper.user_id,
        product_id=product.product_id,
        user_name=shopper.name,
        product_name=product.name,
        product_tags=product.tags,
        occurred_at_utc=occurred_at_utc,
        rating=rating,
    )


def advance_cursor(rng: random.Random, cursor: datetime, min_seconds: int, max_seconds: int) -> datetime:
    return cursor + timedelta(seconds=rng.randint(min_seconds, max_seconds))


def random_rating(rng: random.Random) -> int:
    roll = rng.random()
    if roll < 0.55:
        return 5
    if roll < 0.85:
        return 4
    return 3


def count_events(events: Any) -> dict[str, int]:
    counts: dict[str, int] = {}
    for event in events:
        counts[event.event_type] = counts.get(event.event_type, 0) + 1
    return counts


def compact_tags(*values: Any) -> list[str]:
    seen: set[str] = set()
    result: list[str] = []
    for value in values:
        if value is None:
            continue
        tag = str(value).strip()
        if not tag:
            continue
        tag_key = tag.lower()
        if tag_key in seen:
            continue
        seen.add(tag_key)
        result.append(tag)
    return result


def to_iso8601_utc(value: datetime) -> str:
    return value.astimezone(timezone.utc).isoformat(timespec="milliseconds").replace("+00:00", "Z")


def join_url(base_url: str, path: str) -> str:
    return f"{base_url.rstrip('/')}/{path.lstrip('/')}"


def http_json(
    method: str,
    url: str,
    *,
    timeout_seconds: int,
    payload: dict[str, Any] | None = None,
) -> Any:
    encoded_payload = None
    headers = {"Accept": "application/json"}

    if payload is not None:
        encoded_payload = json.dumps(payload).encode("utf-8")
        headers["Content-Type"] = "application/json"

    req = request.Request(url=url, method=method.upper(), data=encoded_payload, headers=headers)
    with request.urlopen(req, timeout=timeout_seconds) as response:
        raw = response.read()
        if not raw:
            return None
        return json.loads(raw.decode("utf-8"))


if __name__ == "__main__":
    raise SystemExit(main())
