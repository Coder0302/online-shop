using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.App.Migrations
{
    /// <inheritdoc />
    public partial class new_mig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "crm");

            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.EnsureSchema(
                name: "sales");

            migrationBuilder.EnsureSchema(
                name: "pricing");

            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateTable(
                name: "brands",
                schema: "catalog",
                columns: table => new
                {
                    brand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("brands_pkey", x => x.brand_id);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                schema: "catalog",
                columns: table => new
                {
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    slug = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("categories_pkey", x => x.category_id);
                    table.ForeignKey(
                        name: "categories_parent_id_fkey",
                        column: x => x.parent_id,
                        principalSchema: "catalog",
                        principalTable: "categories",
                        principalColumn: "category_id");
                });

            migrationBuilder.CreateTable(
                name: "price_lists",
                schema: "pricing",
                columns: table => new
                {
                    price_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    currency = table.Column<string>(type: "char(3)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("price_lists_pkey", x => x.price_list_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "auth",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    phone_e164 = table.Column<string>(type: "text", nullable: true),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    is_email_verified = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_pkey", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "warehouses",
                schema: "inventory",
                columns: table => new
                {
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    country_code = table.Column<string>(type: "char(2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("warehouses_pkey", x => x.warehouse_id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                schema: "catalog",
                columns: table => new
                {
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sku_base = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    attrs = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("products_pkey", x => x.product_id);
                    table.ForeignKey(
                        name: "products_brand_id_fkey",
                        column: x => x.brand_id,
                        principalSchema: "catalog",
                        principalTable: "brands",
                        principalColumn: "brand_id");
                    table.ForeignKey(
                        name: "products_category_id_fkey",
                        column: x => x.category_id,
                        principalSchema: "catalog",
                        principalTable: "categories",
                        principalColumn: "category_id");
                });

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "crm",
                columns: table => new
                {
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    gender = table.Column<string>(type: "text", nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("customers_pkey", x => x.customer_id);
                    table.ForeignKey(
                        name: "customers_user_id_fkey",
                        column: x => x.user_id,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_media",
                schema: "catalog",
                columns: table => new
                {
                    media_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("product_media_pkey", x => x.media_id);
                    table.ForeignKey(
                        name: "product_media_product_id_fkey",
                        column: x => x.product_id,
                        principalSchema: "catalog",
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "variants",
                schema: "catalog",
                columns: table => new
                {
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "text", nullable: false),
                    option_kv = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    barcode = table.Column<string>(type: "text", nullable: true),
                    weight_g = table.Column<int>(type: "integer", nullable: true),
                    dimensions_mm = table.Column<int[]>(type: "integer[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("variants_pkey", x => x.variant_id);
                    table.ForeignKey(
                        name: "variants_product_id_fkey",
                        column: x => x.product_id,
                        principalSchema: "catalog",
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "addresses",
                schema: "crm",
                columns: table => new
                {
                    address_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    country_code = table.Column<string>(type: "char(2)", nullable: false),
                    region = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "text", nullable: false),
                    street = table.Column<string>(type: "text", nullable: false),
                    zip = table.Column<string>(type: "text", nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("addresses_pkey", x => x.address_id);
                    table.ForeignKey(
                        name: "addresses_customer_id_fkey",
                        column: x => x.customer_id,
                        principalSchema: "crm",
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "carts",
                schema: "sales",
                columns: table => new
                {
                    cart_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    currency = table.Column<string>(type: "char(3)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("carts_pkey", x => x.cart_id);
                    table.ForeignKey(
                        name: "carts_customer_id_fkey",
                        column: x => x.customer_id,
                        principalSchema: "crm",
                        principalTable: "customers",
                        principalColumn: "customer_id");
                });

            migrationBuilder.CreateTable(
                name: "prices",
                schema: "pricing",
                columns: table => new
                {
                    price_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("prices_pkey", x => x.price_id);
                    table.ForeignKey(
                        name: "prices_price_list_id_fkey",
                        column: x => x.price_list_id,
                        principalSchema: "pricing",
                        principalTable: "price_lists",
                        principalColumn: "price_list_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "prices_variant_id_fkey",
                        column: x => x.variant_id,
                        principalSchema: "catalog",
                        principalTable: "variants",
                        principalColumn: "variant_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_items",
                schema: "inventory",
                columns: table => new
                {
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    qty_on_hand = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    qty_reserved = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("stock_items_pkey", x => new { x.warehouse_id, x.variant_id });
                    table.ForeignKey(
                        name: "stock_items_variant_id_fkey",
                        column: x => x.variant_id,
                        principalSchema: "catalog",
                        principalTable: "variants",
                        principalColumn: "variant_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "stock_items_warehouse_id_fkey",
                        column: x => x.warehouse_id,
                        principalSchema: "inventory",
                        principalTable: "warehouses",
                        principalColumn: "warehouse_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cart_items",
                schema: "sales",
                columns: table => new
                {
                    cart_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cart_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    qty = table.Column<int>(type: "integer", nullable: false),
                    price_snapshot = table.Column<decimal>(type: "numeric(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("cart_items_pkey", x => x.cart_item_id);
                    table.ForeignKey(
                        name: "cart_items_cart_id_fkey",
                        column: x => x.cart_id,
                        principalSchema: "sales",
                        principalTable: "carts",
                        principalColumn: "cart_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "cart_items_variant_id_fkey",
                        column: x => x.variant_id,
                        principalSchema: "catalog",
                        principalTable: "variants",
                        principalColumn: "variant_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_addresses_default_one",
                schema: "crm",
                table: "addresses",
                column: "customer_id",
                unique: true,
                filter: "is_default");

            migrationBuilder.CreateIndex(
                name: "brands_name_key",
                schema: "catalog",
                table: "brands",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "cart_items_cart_id_variant_id_key",
                schema: "sales",
                table: "cart_items",
                columns: new[] { "cart_id", "variant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_cart_items_variant",
                schema: "sales",
                table: "cart_items",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "idx_carts_customer",
                schema: "sales",
                table: "carts",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "categories_slug_key",
                schema: "catalog",
                table: "categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_parent_id",
                schema: "catalog",
                table: "categories",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "customers_user_id_key",
                schema: "crm",
                table: "customers",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "price_lists_code_key",
                schema: "pricing",
                table: "price_lists",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_prices_lookup",
                schema: "pricing",
                table: "prices",
                columns: new[] { "variant_id", "price_list_id", "valid_from" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prices_price_list_id",
                schema: "pricing",
                table: "prices",
                column: "price_list_id");

            migrationBuilder.CreateIndex(
                name: "idx_media_product_kind_sort",
                schema: "catalog",
                table: "product_media",
                columns: new[] { "product_id", "kind", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "idx_products_active",
                schema: "catalog",
                table: "products",
                column: "status",
                filter: "(status = 'active')");

            migrationBuilder.CreateIndex(
                name: "idx_products_attrs_gin",
                schema: "catalog",
                table: "products",
                column: "attrs")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_products_brand_id",
                schema: "catalog",
                table: "products",
                column: "brand_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_category_id",
                schema: "catalog",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "idx_stock_items_variant",
                schema: "inventory",
                table: "stock_items",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "users_phone_e164_key",
                schema: "auth",
                table: "users",
                column: "phone_e164",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_variants_optionkv_gin",
                schema: "catalog",
                table: "variants",
                column: "option_kv")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_variants_product_id",
                schema: "catalog",
                table: "variants",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "variants_sku_key",
                schema: "catalog",
                table: "variants",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "warehouses_code_key",
                schema: "inventory",
                table: "warehouses",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "addresses",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "cart_items",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "prices",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "product_media",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "stock_items",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "carts",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "price_lists",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "variants",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "warehouses",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "customers",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "products",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "users",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "brands",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "categories",
                schema: "catalog");
        }
    }
}
