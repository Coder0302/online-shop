using Microsoft.EntityFrameworkCore;

namespace ECommerce.Data.Migrations;
public static class InitAndFixes
{
    public static async Task ApplyAsync(ECommerceDbContext db, CancellationToken ct = default)
    {
        var sql = @"
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS btree_gist;

CREATE SCHEMA auth AUTHORIZATION pg; 
CREATE TABLE auth.users ( 
	user_id uuid DEFAULT gen_random_uuid() NOT NULL,
	phone_e164 text NULL,
	password_hash text NOT NULL,
	is_email_verified bool DEFAULT false NOT NULL,
	created_at timestamptz DEFAULT now() NOT NULL,
	CONSTRAINT users_phone_e164_key UNIQUE (phone_e164),
	CONSTRAINT users_pkey PRIMARY KEY (user_id)
);

CREATE SCHEMA ""catalog"" AUTHORIZATION pg;

CREATE TABLE ""catalog"".brands (
	brand_id uuid DEFAULT gen_random_uuid() NOT NULL,
	""name"" text NOT NULL,
	CONSTRAINT brands_name_key UNIQUE (name),
	CONSTRAINT brands_pkey PRIMARY KEY (brand_id)
);

CREATE TABLE ""catalog"".categories (
	category_id uuid DEFAULT gen_random_uuid() NOT NULL,
	parent_id uuid NULL,
	slug text NOT NULL,
	""name"" text NOT NULL,
	CONSTRAINT categories_pkey PRIMARY KEY (category_id),
	CONSTRAINT categories_slug_key UNIQUE (slug),
	CONSTRAINT categories_parent_id_fkey FOREIGN KEY (parent_id) REFERENCES ""catalog"".categories(category_id)
);

CREATE TABLE ""catalog"".products (
	product_id uuid DEFAULT gen_random_uuid() NOT NULL,
	brand_id uuid NULL,
	category_id uuid NULL,
	sku_base text NOT NULL,
	""name"" text NOT NULL,
	status text DEFAULT 'draft'::text NOT NULL,
	attrs jsonb DEFAULT '{}'::jsonb NOT NULL,
	created_at timestamptz DEFAULT now() NOT NULL,
	CONSTRAINT products_pkey PRIMARY KEY (product_id),
	CONSTRAINT products_status_check CHECK ((status = ANY (ARRAY['draft'::text, 'active'::text, 'archived'::text]))),
	CONSTRAINT products_brand_id_fkey FOREIGN KEY (brand_id) REFERENCES ""catalog"".brands(brand_id),
	CONSTRAINT products_category_id_fkey FOREIGN KEY (category_id) REFERENCES ""catalog"".categories(category_id)
);
CREATE INDEX idx_products_active ON catalog.products USING btree (status) WHERE (status = 'active'::text);
CREATE INDEX idx_products_attrs_gin ON catalog.products USING gin (attrs);

CREATE TABLE ""catalog"".variants (
	variant_id uuid DEFAULT gen_random_uuid() NOT NULL,
	product_id uuid NOT NULL,
	sku text NOT NULL,
	option_kv jsonb DEFAULT '{}'::jsonb NOT NULL,
	barcode text NULL,
	weight_g int4 NULL,
	dimensions_mm _int4 NULL,
	CONSTRAINT variants_dimensions_mm_check CHECK ((array_length(dimensions_mm, 1) = 3)),
	CONSTRAINT variants_pkey PRIMARY KEY (variant_id),
	CONSTRAINT variants_sku_key UNIQUE (sku),
	CONSTRAINT variants_product_id_fkey FOREIGN KEY (product_id) REFERENCES ""catalog"".products(product_id) ON DELETE CASCADE
);
CREATE INDEX idx_variants_optionkv_gin ON catalog.variants USING gin (option_kv);

CREATE TABLE ""catalog"".product_media (
	media_id uuid DEFAULT gen_random_uuid() NOT NULL,
	product_id uuid NOT NULL,
	url text NOT NULL,
	kind text NOT NULL,
	sort_order int4 DEFAULT 0 NOT NULL,
	CONSTRAINT product_media_kind_check CHECK ((kind = ANY (ARRAY['image'::text, 'video'::text, 'manual'::text]))),
	CONSTRAINT product_media_pkey PRIMARY KEY (media_id),
	CONSTRAINT product_media_product_id_fkey FOREIGN KEY (product_id) REFERENCES ""catalog"".products(product_id) ON DELETE CASCADE
);

CREATE SCHEMA crm AUTHORIZATION pg;

CREATE TABLE crm.addresses ( address_id uuid DEFAULT gen_random_uuid() NOT NULL, customer_id uuid NOT NULL, country_code bpchar(2) NOT NULL, region text NULL, city text NOT NULL, street text NOT NULL, zip text NULL, is_default bool DEFAULT false NOT NULL, CONSTRAINT addresses_pkey PRIMARY KEY (address_id));
CREATE INDEX idx_addresses_customer_default ON crm.addresses USING btree (customer_id) WHERE is_default;

CREATE TABLE crm.customers ( customer_id uuid DEFAULT gen_random_uuid() NOT NULL, user_id uuid NULL, first_name text NOT NULL, last_name text NOT NULL, gender text DEFAULT 'U'::text NOT NULL, birth_date date NULL, created_at timestamptz DEFAULT now() NOT NULL, CONSTRAINT customers_gender_check CHECK ((gender = ANY (ARRAY['M'::text, 'F'::text, 'U'::text]))), CONSTRAINT customers_pkey PRIMARY KEY (customer_id), CONSTRAINT customers_user_id_key UNIQUE (user_id));

ALTER TABLE crm.addresses ADD CONSTRAINT addresses_customer_id_fkey FOREIGN KEY (customer_id) REFERENCES crm.customers(customer_id) ON DELETE CASCADE;

ALTER TABLE crm.customers ADD CONSTRAINT customers_user_id_fkey FOREIGN KEY (user_id) REFERENCES auth.users(user_id) ON DELETE CASCADE;

CREATE SCHEMA inventory AUTHORIZATION pg;

CREATE TABLE inventory.warehouses ( warehouse_id uuid DEFAULT gen_random_uuid() NOT NULL, code text NOT NULL, ""name"" text NOT NULL, country_code bpchar(2) NOT NULL, CONSTRAINT warehouses_code_key UNIQUE (code), CONSTRAINT warehouses_pkey PRIMARY KEY (warehouse_id));

CREATE TABLE inventory.stock_items ( warehouse_id uuid NOT NULL, variant_id uuid NOT NULL, qty_on_hand int4 DEFAULT 0 NOT NULL, qty_reserved int4 DEFAULT 0 NOT NULL, CONSTRAINT stock_items_pkey PRIMARY KEY (warehouse_id, variant_id));
CREATE INDEX idx_stock_items_variant ON inventory.stock_items USING btree (variant_id);

ALTER TABLE inventory.stock_items ADD CONSTRAINT stock_items_variant_id_fkey FOREIGN KEY (variant_id) REFERENCES ""catalog"".variants(variant_id) ON DELETE CASCADE;
ALTER TABLE inventory.stock_items ADD CONSTRAINT stock_items_warehouse_id_fkey FOREIGN KEY (warehouse_id) REFERENCES inventory.warehouses(warehouse_id) ON DELETE CASCADE;

CREATE SCHEMA pricing AUTHORIZATION pg;

CREATE TABLE pricing.price_lists ( price_list_id uuid DEFAULT gen_random_uuid() NOT NULL, code text NOT NULL, currency bpchar(3) NOT NULL, is_active bool DEFAULT true NOT NULL, CONSTRAINT price_lists_code_key UNIQUE (code), CONSTRAINT price_lists_pkey PRIMARY KEY (price_list_id));

CREATE TABLE pricing.prices ( price_id uuid DEFAULT gen_random_uuid() NOT NULL, variant_id uuid NOT NULL, price_list_id uuid NOT NULL, amount numeric(12, 2) NOT NULL, valid_from timestamptz DEFAULT now() NOT NULL, valid_to timestamptz NULL, CONSTRAINT prices_amount_check CHECK ((amount >= (0)::numeric)), CONSTRAINT prices_pkey PRIMARY KEY (price_id), CONSTRAINT prices_variant_id_price_list_id_valid_from_key UNIQUE (variant_id, price_list_id, valid_from));

ALTER TABLE pricing.prices ADD CONSTRAINT prices_price_list_id_fkey FOREIGN KEY (price_list_id) REFERENCES pricing.price_lists(price_list_id) ON DELETE CASCADE;
ALTER TABLE pricing.prices ADD CONSTRAINT prices_variant_id_fkey FOREIGN KEY (variant_id) REFERENCES ""catalog"".variants(variant_id) ON DELETE CASCADE;

CREATE SCHEMA sales AUTHORIZATION pg;

CREATE TABLE sales.cart_items (
	cart_item_id uuid DEFAULT gen_random_uuid() NOT NULL,
	cart_id uuid NOT NULL,
	variant_id uuid NOT NULL,
	qty int4 NOT NULL,
	price_snapshot numeric(12, 2) NOT NULL,
	CONSTRAINT cart_items_cart_id_variant_id_key UNIQUE (cart_id, variant_id),
	CONSTRAINT cart_items_pkey PRIMARY KEY (cart_item_id),
	CONSTRAINT cart_items_qty_check CHECK ((qty > 0))
);

CREATE TABLE sales.carts (
	cart_id uuid DEFAULT gen_random_uuid() NOT NULL,
	customer_id uuid NULL,
	currency bpchar(3) NOT NULL,
	created_at timestamptz DEFAULT now() NOT NULL,
	updated_at timestamptz DEFAULT now() NOT NULL,
	CONSTRAINT carts_pkey PRIMARY KEY (cart_id)
);

ALTER TABLE sales.cart_items ADD CONSTRAINT cart_items_cart_id_fkey FOREIGN KEY (cart_id) REFERENCES sales.carts(cart_id) ON DELETE CASCADE;
ALTER TABLE sales.cart_items ADD CONSTRAINT cart_items_variant_id_fkey FOREIGN KEY (variant_id) REFERENCES ""catalog"".variants(variant_id);

ALTER TABLE sales.carts ADD CONSTRAINT carts_customer_id_fkey FOREIGN KEY (customer_id) REFERENCES crm.customers(customer_id);
";
        await db.Database.ExecuteSqlRawAsync(sql, ct);
    }
}
