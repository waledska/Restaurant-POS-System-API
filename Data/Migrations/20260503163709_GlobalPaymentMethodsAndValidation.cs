using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApisApp.Data.Migrations
{
    public partial class GlobalPaymentMethodsAndValidation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_SupplierPaymentMethods_SupplierPaymentMethodId",
                table: "SupplierPayments");

            migrationBuilder.DropTable(
                name: "SupplierPaymentMethods");

            migrationBuilder.RenameColumn(
                name: "SupplierPaymentMethodId",
                table: "SupplierPayments",
                newName: "SystemPaymentMethodId");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierPayments_SupplierPaymentMethodId",
                table: "SupplierPayments",
                newName: "IX_SupplierPayments_SystemPaymentMethodId");

            migrationBuilder.AlterColumn<decimal>(
                name: "DifferenceQty",
                table: "StockCountItems",
                type: "decimal(18,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "ActualQty",
                table: "StockCountItems",
                type: "decimal(18,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)");

            migrationBuilder.CreateTable(
                name: "SystemPaymentMethods",
                columns: table => new
                {
                    SystemPaymentMethodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MethodType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemPaymentMethods", x => x.SystemPaymentMethodId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemPaymentMethods_MethodType_IsActive",
                table: "SystemPaymentMethods",
                columns: new[] { "MethodType", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_SystemPaymentMethods_SystemPaymentMethodId",
                table: "SupplierPayments",
                column: "SystemPaymentMethodId",
                principalTable: "SystemPaymentMethods",
                principalColumn: "SystemPaymentMethodId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_SystemPaymentMethods_SystemPaymentMethodId",
                table: "SupplierPayments");

            migrationBuilder.DropTable(
                name: "SystemPaymentMethods");

            migrationBuilder.RenameColumn(
                name: "SystemPaymentMethodId",
                table: "SupplierPayments",
                newName: "SupplierPaymentMethodId");

            migrationBuilder.RenameIndex(
                name: "IX_SupplierPayments_SystemPaymentMethodId",
                table: "SupplierPayments",
                newName: "IX_SupplierPayments_SupplierPaymentMethodId");

            migrationBuilder.AlterColumn<decimal>(
                name: "DifferenceQty",
                table: "StockCountItems",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ActualQty",
                table: "StockCountItems",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "SupplierPaymentMethods",
                columns: table => new
                {
                    SupplierPaymentMethodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    MethodType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierPaymentMethods", x => x.SupplierPaymentMethodId);
                    table.ForeignKey(
                        name: "FK_SupplierPaymentMethods_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "SupplierId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPaymentMethods_SupplierId",
                table: "SupplierPaymentMethods",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPaymentMethods_SupplierId_MethodType_IsActive",
                table: "SupplierPaymentMethods",
                columns: new[] { "SupplierId", "MethodType", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_SupplierPaymentMethods_SupplierPaymentMethodId",
                table: "SupplierPayments",
                column: "SupplierPaymentMethodId",
                principalTable: "SupplierPaymentMethods",
                principalColumn: "SupplierPaymentMethodId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
