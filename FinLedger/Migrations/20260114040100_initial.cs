using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinLedger.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Data = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", maxLength: 36, nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WebhookUrl = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FailureReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.CheckConstraint("CK_Amount_Positive", "\"Amount\" > 0");
                    table.CheckConstraint("CK_Status_Valid", "\"Status\" IN ('Pending','Completed','Failed','Cancelled')");
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalPayments = table.Column<int>(type: "integer", nullable: false),
                    PendingPayments = table.Column<int>(type: "integer", nullable: false),
                    CompletedPayments = table.Column<int>(type: "integer", nullable: false),
                    FailedPayments = table.Column<int>(type: "integer", nullable: false),
                    CustomerBalance = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    ClearingBalance = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    MerchantBalance = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    IsBalanced = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookAggregates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookAggregates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Account = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Debit = table.Column<int>(type: "integer", nullable: false),
                    Credit = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    TransactionHash = table.Column<string>(type: "character varying(88)", maxLength: 88, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LedgerEntries_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebhookDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EncryptedPayload = table.Column<string>(type: "text", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsSuccessful = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WebhookAggregateId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebhookDeliveries_WebhookAggregates_WebhookAggregateId",
                        column: x => x.WebhookAggregateId,
                        principalTable: "WebhookAggregates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_AggregateId",
                table: "Events",
                column: "AggregateId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_AggregateId_CreatedAt",
                table: "Events",
                columns: new[] { "AggregateId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_CreatedAt",
                table: "Events",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventType",
                table: "Events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_Account",
                table: "LedgerEntries",
                column: "Account");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_PaymentId",
                table: "LedgerEntries",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_PaymentId_CreatedAt",
                table: "LedgerEntries",
                columns: new[] { "PaymentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CreatedAt",
                table: "Payments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MerchantId",
                table: "Payments",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Reference",
                table: "Payments",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status",
                table: "Payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationSnapshots_CreatedAt",
                table: "ReconciliationSnapshots",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookAggregates_CreatedAt",
                table: "WebhookAggregates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookAggregates_PaymentId",
                table: "WebhookAggregates",
                column: "PaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_NextRetryAt",
                table: "WebhookDeliveries",
                column: "NextRetryAt");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_PaymentId",
                table: "WebhookDeliveries",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_WebhookAggregateId",
                table: "WebhookDeliveries",
                column: "WebhookAggregateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "LedgerEntries");

            migrationBuilder.DropTable(
                name: "ReconciliationSnapshots");

            migrationBuilder.DropTable(
                name: "WebhookDeliveries");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "WebhookAggregates");
        }
    }
}
