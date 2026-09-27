using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Soransoft.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// تبدیل «اقساط» به «مراحل پرداخت» (بدون از دست رفتن داده، با Rename) و
    /// افزودن کاتالوگ هزینه‌های شخص ثالث + اعمال آن روی قرارداد و پرچم سقف نامحدود قرارداد همکاری.
    /// </summary>
    public partial class AddPaymentStagesAndThirdPartyCosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---------- قرارداد همکاری: پرچم سقف نامحدود + تغییر نام فلگ مبنا ----------
            migrationBuilder.AddColumn<bool>(
                name: "IsUnlimitedAmount",
                table: "CooperationAgreements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.RenameColumn(
                name: "BasedOnPaidInstallments",
                table: "CooperationAgreements",
                newName: "BasedOnPaidStages");

            migrationBuilder.RenameColumn(
                name: "BasedOnPaidInstallments",
                table: "CommissionRates",
                newName: "BasedOnPaidStages");

            // ---------- اقساط → مراحل پرداخت (حفظ داده با تغییر نام جدول) ----------
            migrationBuilder.RenameTable(
                name: "ContractInstallments",
                newName: "ContractPaymentStages");

            migrationBuilder.RenameIndex(
                name: "IX_ContractInstallments_ContractId_Number",
                table: "ContractPaymentStages",
                newName: "IX_ContractPaymentStages_ContractId_Number");

            // نام قیدها پس از RenameTable در SQL Server تغییر نمی‌کند؛ هم‌نامی با مدل برای جلوگیری از اختلاف در مهاجرت‌های بعدی
            migrationBuilder.Sql("EXEC sp_rename N'[PK_ContractInstallments]', N'PK_ContractPaymentStages';");
            migrationBuilder.Sql("EXEC sp_rename N'[FK_ContractInstallments_PartnerContracts_ContractId]', N'FK_ContractPaymentStages_PartnerContracts_ContractId';");

            // ---------- ستون‌های جدید مرحله پرداخت ----------
            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "ContractPaymentStages",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ContractPaymentStages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PercentOfTotal",
                table: "ContractPaymentStages",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentFile",
                table: "ContractPaymentStages",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentNote",
                table: "ContractPaymentStages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChequeNo",
                table: "ContractPaymentStages",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChequeBank",
                table: "ContractPaymentStages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ChequeDueDate",
                table: "ContractPaymentStages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DocumentsVerifiedAt",
                table: "ContractPaymentStages",
                type: "datetime2",
                nullable: true);

            // ---------- کاتالوگ هزینه‌های شخص ثالث ----------
            migrationBuilder.CreateTable(
                name: "ThirdPartyCostItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TechnicalSpecs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultAmount = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThirdPartyCostItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractThirdPartyCosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TechnicalSpecs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeductFromFirstPayment = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractThirdPartyCosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractThirdPartyCosts_PartnerContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "PartnerContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractThirdPartyCosts_ThirdPartyCostItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ThirdPartyCostItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractThirdPartyCosts_ContractId",
                table: "ContractThirdPartyCosts",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractThirdPartyCosts_ItemId",
                table: "ContractThirdPartyCosts",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ThirdPartyCostItems_Type",
                table: "ThirdPartyCostItems",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractThirdPartyCosts");

            migrationBuilder.DropTable(
                name: "ThirdPartyCostItems");

            migrationBuilder.RenameIndex(
                name: "IX_ContractPaymentStages_ContractId_Number",
                table: "ContractPaymentStages",
                newName: "IX_ContractInstallments_ContractId_Number");

            migrationBuilder.RenameTable(
                name: "ContractPaymentStages",
                newName: "ContractInstallments");

            migrationBuilder.Sql("EXEC sp_rename N'[PK_ContractPaymentStages]', N'PK_ContractInstallments';");
            migrationBuilder.Sql("EXEC sp_rename N'[FK_ContractPaymentStages_PartnerContracts_ContractId]', N'FK_ContractInstallments_PartnerContracts_ContractId';");

            migrationBuilder.DropColumn(name: "Title", table: "ContractInstallments");
            migrationBuilder.DropColumn(name: "Description", table: "ContractInstallments");
            migrationBuilder.DropColumn(name: "PercentOfTotal", table: "ContractInstallments");
            migrationBuilder.DropColumn(name: "DocumentFile", table: "ContractInstallments");
            migrationBuilder.DropColumn(name: "DocumentNote", table: "ContractInstallments");
            migrationBuilder.DropColumn(name: "ChequeNo", table: "ContractInstallments");
            migrationBuilder.DropColumn(name: "ChequeBank", table: "ContractInstallments");
            migrationBuilder.DropColumn(name: "ChequeDueDate", table: "ContractInstallments");
            migrationBuilder.DropColumn(name: "DocumentsVerifiedAt", table: "ContractInstallments");

            migrationBuilder.RenameColumn(
                name: "BasedOnPaidStages",
                table: "CooperationAgreements",
                newName: "BasedOnPaidInstallments");

            migrationBuilder.RenameColumn(
                name: "BasedOnPaidStages",
                table: "CommissionRates",
                newName: "BasedOnPaidInstallments");

            migrationBuilder.DropColumn(name: "IsUnlimitedAmount", table: "CooperationAgreements");
        }
    }
}
