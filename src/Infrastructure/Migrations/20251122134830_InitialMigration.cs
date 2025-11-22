using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_number = table.Column<string>(type: "text", nullable: false),
                    gateway_order_id = table.Column<string>(type: "text", nullable: true),
                    amount_value = table.Column<decimal>(type: "numeric", nullable: false),
                    amount_currency = table.Column<string>(type: "text", nullable: false),
                    method = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    idempotency_key = table.Column<string>(type: "text", nullable: false),
                    tenant_key = table.Column<string>(type: "text", nullable: true),
                    error_code = table.Column<string>(type: "text", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payments_gateway_order_id",
                table: "payments",
                column: "gateway_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_idempotency_key_tenant_key",
                table: "payments",
                columns: new[] { "idempotency_key", "tenant_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payments_order_number",
                table: "payments",
                column: "order_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payments");
        }
    }
}
