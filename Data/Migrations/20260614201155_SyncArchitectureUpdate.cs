using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApisApp.Data.Migrations
{
    public partial class SyncArchitectureUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OriginLocationId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginLocationId",
                table: "TransferRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginLocationId",
                table: "TransferRequestItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginLocationId",
                table: "SystemPaymentMethods",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginLocationId",
                table: "Suppliers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginLocationId",
                table: "SupplierPayments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginLocationId",
                table: "StockMovements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginLocationId",
                table: "StockCounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginLocationId",
                table: "StockCountItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginLocationId",
                table: "StockBalances",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginLocationId",
                table: "ServerChangeLog",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginLocationId",
                table: "PurchaseInvoices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginLocationId",
                table: "PurchaseInvoiceItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginLocationId",
                table: "Products",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginLocationId",
                table: "ProductRecipes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginLocationId",
                table: "ProductionOperations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginLocationId",
                table: "ProductionOperationItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginLocationId",
                table: "Locations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceRole",
                table: "Devices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MachineName",
                table: "Devices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                table: "AuditLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GlobalSettings",
                columns: table => new
                {
                    SettingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettingKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SettingValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    OriginLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalSettings", x => x.SettingId);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SubscriptionPlan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SubscriptionEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    OriginLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.TenantId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_LocationId",
                table: "AuditLogs",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "UX_GlobalSettings_SettingKey",
                table: "GlobalSettings",
                column: "SettingKey",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Locations_LocationId",
                table: "AuditLogs",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "LocationId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Locations_LocationId",
                table: "AuditLogs");

            migrationBuilder.DropTable(
                name: "GlobalSettings");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_LocationId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "OriginLocationId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OriginLocationId",
                table: "TransferRequests");

            migrationBuilder.DropColumn(
                name: "OriginLocationId",
                table: "TransferRequestItems");

            migrationBuilder.DropColumn(
                name: "OriginLocationId",
                table: "SystemPaymentMethods");

            migrationBuilder.DropColumn(
                name: "OriginLocationId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "OriginLocationId",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "OriginLocationId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "OriginLocationId",
                table: "StockCounts");

            migrationBuilder.DropColumn(
                name: "OriginLocationId",
                table: "StockCountItems");

            migrationBuilder.DropColumn(
                name: "OriginLocationId",
                table: "StockBalances");

            migrationBuilder.DropColumn(
                name: "OriginLocationId",
                table: "ServerChangeLog");

            migrationBuilder.DropColumn(
                name: "OriginLocationId",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "OriginLocationId",
                table: "PurchaseInvoiceItems");

            migrationBuilder.DropColumn(
                name: "OriginLocationId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "OriginLocationId",
                table: "ProductRecipes");

            migrationBuilder.DropColumn(
                name: "OriginLocationId",
                table: "ProductionOperations");

            migrationBuilder.DropColumn(
                name: "OriginLocationId",
                table: "ProductionOperationItems");

            migrationBuilder.DropColumn(
                name: "OriginLocationId",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "DeviceRole",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "MachineName",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "AuditLogs");
        }
    }
}
