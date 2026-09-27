using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Soransoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAgreementsAndWallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CooperationAgreements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PartnerId = table.Column<int>(type: "int", nullable: false),
                    AgreementNo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Terms = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalAmount = table.Column<long>(type: "bigint", nullable: true),
                    CommissionMethod = table.Column<int>(type: "int", nullable: false),
                    CommissionValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TiersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BasedOnPaidInstallments = table.Column<bool>(type: "bit", nullable: false),
                    MonthlySalary = table.Column<long>(type: "bigint", nullable: true),
                    SettlementTerms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BankAccountIban = table.Column<string>(type: "nvarchar(34)", maxLength: 34, nullable: true),
                    BankAccountHolder = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ContractFile = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AdminNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CooperationAgreements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CooperationAgreements_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WalletTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartnerId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    DocumentFile = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    BankTrackingNo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    WithdrawalRequestId = table.Column<int>(type: "int", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WithdrawalRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartnerId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DestinationIban = table.Column<string>(type: "nvarchar(34)", maxLength: 34, nullable: false),
                    DestinationBank = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DestinationHolder = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AdminResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WalletTransactionId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WithdrawalRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WithdrawalRequests_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WithdrawalRequests_WalletTransactions_WalletTransactionId",
                        column: x => x.WalletTransactionId,
                        principalTable: "WalletTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CooperationAgreements_AgreementNo",
                table: "CooperationAgreements",
                column: "AgreementNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CooperationAgreements_PartnerId_Kind",
                table: "CooperationAgreements",
                columns: new[] { "PartnerId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_CreatedAt",
                table: "WalletTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_PartnerId_Status",
                table: "WalletTransactions",
                columns: new[] { "PartnerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequests_PartnerId",
                table: "WithdrawalRequests",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequests_Status",
                table: "WithdrawalRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequests_WalletTransactionId",
                table: "WithdrawalRequests",
                column: "WalletTransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CooperationAgreements");

            migrationBuilder.DropTable(
                name: "WithdrawalRequests");

            migrationBuilder.DropTable(
                name: "WalletTransactions");
        }
    }
}
