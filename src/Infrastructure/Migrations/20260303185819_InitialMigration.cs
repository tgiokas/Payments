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
                    description = table.Column<string>(type: "text", nullable: true),
                    order_number = table.Column<string>(type: "text", nullable: false),
                    gateway_order_id = table.Column<string>(type: "text", nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    method = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    idempotency_key = table.Column<string>(type: "text", nullable: false),
                    bank_name = table.Column<string>(type: "text", nullable: true),
                    bank_country_code = table.Column<string>(type: "text", nullable: true),
                    bank_country_name = table.Column<string>(type: "text", nullable: true),
                    masked_pan = table.Column<string>(type: "text", nullable: true),
                    expiration = table.Column<string>(type: "text", nullable: true),
                    cardholder_name = table.Column<string>(type: "text", nullable: true),
                    approval_code = table.Column<string>(type: "text", nullable: true),
                    payment_system = table.Column<string>(type: "text", nullable: true),
                    payment_state = table.Column<string>(type: "text", nullable: true),
                    approved_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    deposited_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    refunded_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    fee_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    payment_way = table.Column<string>(type: "text", nullable: true),
                    action_code = table.Column<string>(type: "text", nullable: true),
                    order_status = table.Column<int>(type: "integer", nullable: false),
                    error_code = table.Column<string>(type: "text", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                name: "ix_payments_idempotency_key",
                table: "payments",
                column: "idempotency_key",
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
