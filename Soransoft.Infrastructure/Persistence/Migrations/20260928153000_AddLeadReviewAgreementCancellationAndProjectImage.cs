using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Soransoft.Infrastructure.Persistence;

#nullable disable

namespace Soransoft.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoransoftDbContext))]
[Migration("20260928153000_AddLeadReviewAgreementCancellationAndProjectImage")]
public partial class AddLeadReviewAgreementCancellationAndProjectImage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "NationalId", table: "Partners", type: "nvarchar(20)", maxLength: 20, nullable: true);

        migrationBuilder.AddColumn<string>(name: "FormNo", table: "Leads", type: "nvarchar(40)", maxLength: 40, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "IntroducedAt", table: "Leads", type: "datetime2", nullable: false, defaultValueSql: "GETDATE()");
        migrationBuilder.AddColumn<string>(name: "CustomerCode", table: "Leads", type: "nvarchar(50)", maxLength: 50, nullable: true);
        migrationBuilder.AddColumn<bool>(name: "IsFollowUp", table: "Leads", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<string>(name: "SalesPartnerNationalId", table: "Leads", type: "nvarchar(20)", maxLength: 20, nullable: true);
        migrationBuilder.AddColumn<string>(name: "SalesPartnerAgreementNo", table: "Leads", type: "nvarchar(60)", maxLength: 60, nullable: true);
        migrationBuilder.AddColumn<string>(name: "SalesPartnerBankAccount", table: "Leads", type: "nvarchar(80)", maxLength: 80, nullable: true);
        migrationBuilder.AddColumn<string>(name: "BusinessName", table: "Leads", type: "nvarchar(200)", maxLength: 200, nullable: true);
        migrationBuilder.AddColumn<string>(name: "DecisionMakerName", table: "Leads", type: "nvarchar(150)", maxLength: 150, nullable: true);
        migrationBuilder.AddColumn<string>(name: "DecisionMakerRole", table: "Leads", type: "nvarchar(120)", maxLength: 120, nullable: true);
        migrationBuilder.AddColumn<string>(name: "CustomerLandline", table: "Leads", type: "nvarchar(30)", maxLength: 30, nullable: true);
        migrationBuilder.AddColumn<string>(name: "CustomerEmail", table: "Leads", type: "nvarchar(200)", maxLength: 200, nullable: true);
        migrationBuilder.AddColumn<string>(name: "CurrentWebsite", table: "Leads", type: "nvarchar(300)", maxLength: 300, nullable: true);
        migrationBuilder.AddColumn<string>(name: "Province", table: "Leads", type: "nvarchar(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<string>(name: "City", table: "Leads", type: "nvarchar(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<string>(name: "FullAddress", table: "Leads", type: "nvarchar(max)", nullable: true);
        migrationBuilder.AddColumn<string>(name: "SocialMedia", table: "Leads", type: "nvarchar(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<bool>(name: "HasWebsite", table: "Leads", type: "bit", nullable: true);
        migrationBuilder.AddColumn<string>(name: "BusinessType", table: "Leads", type: "nvarchar(150)", maxLength: 150, nullable: true);
        migrationBuilder.AddColumn<string>(name: "CurrentSystem", table: "Leads", type: "nvarchar(150)", maxLength: 150, nullable: true);
        migrationBuilder.AddColumn<bool>(name: "HasSimilarPlatform", table: "Leads", type: "bit", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "ExpectedStartDate", table: "Leads", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<long>(name: "BudgetAmount", table: "Leads", type: "bigint", nullable: true);
        migrationBuilder.AddColumn<string>(name: "SecondaryRequirements", table: "Leads", type: "nvarchar(max)", nullable: true);
        migrationBuilder.AddColumn<string>(name: "IntroductionMethod", table: "Leads", type: "nvarchar(200)", maxLength: 200, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "FirstContactDate", table: "Leads", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<bool>(name: "DecisionMakerConfirmed", table: "Leads", type: "bit", nullable: true);
        migrationBuilder.AddColumn<int>(name: "NegotiationLevel", table: "Leads", type: "int", nullable: true);
        migrationBuilder.AddColumn<bool>(name: "NearContract", table: "Leads", type: "bit", nullable: true);
        migrationBuilder.AddColumn<int>(name: "ReviewStatus", table: "Leads", type: "int", nullable: false, defaultValue: 1);
        migrationBuilder.AddColumn<bool>(name: "ExistingCustomer", table: "Leads", type: "bit", nullable: true);
        migrationBuilder.AddColumn<string>(name: "SystemCustomerNo", table: "Leads", type: "nvarchar(80)", maxLength: 80, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "ReviewedAt", table: "Leads", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<int>(name: "ReviewedByAdminId", table: "Leads", type: "int", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ReviewNote", table: "Leads", type: "nvarchar(max)", nullable: true);

        migrationBuilder.AddColumn<string>(name: "Action", table: "LeadHistories", type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "StageChanged");
        migrationBuilder.AddColumn<int>(name: "FromReviewStatus", table: "LeadHistories", type: "int", nullable: true);
        migrationBuilder.AddColumn<int>(name: "ToReviewStatus", table: "LeadHistories", type: "int", nullable: true);
        migrationBuilder.AddColumn<int>(name: "AdminId", table: "LeadHistories", type: "int", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_LeadHistories_LeadId_CreatedAt", table: "LeadHistories", columns: new[] { "LeadId", "CreatedAt" });

        migrationBuilder.AddColumn<string>(name: "FeaturedImage", table: "SellableProjects", type: "nvarchar(300)", maxLength: 300, nullable: true);

        migrationBuilder.CreateTable(
            name: "AgreementCancellationRequests",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                AgreementId = table.Column<int>(type: "int", nullable: false),
                PartnerId = table.Column<int>(type: "int", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                RequestedTerminationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                AdminResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                DecidedByAdminId = table.Column<int>(type: "int", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AgreementCancellationRequests", x => x.Id);
                table.ForeignKey("FK_AgreementCancellationRequests_CooperationAgreements_AgreementId", x => x.AgreementId, "CooperationAgreements", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_AgreementCancellationRequests_Partners_PartnerId", x => x.PartnerId, "Partners", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_AgreementCancellationRequests_AgreementId_Status", table: "AgreementCancellationRequests", columns: new[] { "AgreementId", "Status" });
        migrationBuilder.CreateIndex(name: "IX_AgreementCancellationRequests_PartnerId_Status", table: "AgreementCancellationRequests", columns: new[] { "PartnerId", "Status" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AgreementCancellationRequests");
        migrationBuilder.DropIndex(name: "IX_LeadHistories_LeadId_CreatedAt", table: "LeadHistories");
        migrationBuilder.DropColumn(name: "NationalId", table: "Partners");
        migrationBuilder.DropColumn(name: "FormNo", table: "Leads");
        migrationBuilder.DropColumn(name: "IntroducedAt", table: "Leads");
        migrationBuilder.DropColumn(name: "CustomerCode", table: "Leads");
        migrationBuilder.DropColumn(name: "IsFollowUp", table: "Leads");
        migrationBuilder.DropColumn(name: "SalesPartnerNationalId", table: "Leads");
        migrationBuilder.DropColumn(name: "SalesPartnerAgreementNo", table: "Leads");
        migrationBuilder.DropColumn(name: "SalesPartnerBankAccount", table: "Leads");
        migrationBuilder.DropColumn(name: "BusinessName", table: "Leads");
        migrationBuilder.DropColumn(name: "DecisionMakerName", table: "Leads");
        migrationBuilder.DropColumn(name: "DecisionMakerRole", table: "Leads");
        migrationBuilder.DropColumn(name: "CustomerLandline", table: "Leads");
        migrationBuilder.DropColumn(name: "CustomerEmail", table: "Leads");
        migrationBuilder.DropColumn(name: "CurrentWebsite", table: "Leads");
        migrationBuilder.DropColumn(name: "Province", table: "Leads");
        migrationBuilder.DropColumn(name: "City", table: "Leads");
        migrationBuilder.DropColumn(name: "FullAddress", table: "Leads");
        migrationBuilder.DropColumn(name: "SocialMedia", table: "Leads");
        migrationBuilder.DropColumn(name: "HasWebsite", table: "Leads");
        migrationBuilder.DropColumn(name: "BusinessType", table: "Leads");
        migrationBuilder.DropColumn(name: "CurrentSystem", table: "Leads");
        migrationBuilder.DropColumn(name: "HasSimilarPlatform", table: "Leads");
        migrationBuilder.DropColumn(name: "ExpectedStartDate", table: "Leads");
        migrationBuilder.DropColumn(name: "BudgetAmount", table: "Leads");
        migrationBuilder.DropColumn(name: "SecondaryRequirements", table: "Leads");
        migrationBuilder.DropColumn(name: "IntroductionMethod", table: "Leads");
        migrationBuilder.DropColumn(name: "FirstContactDate", table: "Leads");
        migrationBuilder.DropColumn(name: "DecisionMakerConfirmed", table: "Leads");
        migrationBuilder.DropColumn(name: "NegotiationLevel", table: "Leads");
        migrationBuilder.DropColumn(name: "NearContract", table: "Leads");
        migrationBuilder.DropColumn(name: "ReviewStatus", table: "Leads");
        migrationBuilder.DropColumn(name: "ExistingCustomer", table: "Leads");
        migrationBuilder.DropColumn(name: "SystemCustomerNo", table: "Leads");
        migrationBuilder.DropColumn(name: "ReviewedAt", table: "Leads");
        migrationBuilder.DropColumn(name: "ReviewedByAdminId", table: "Leads");
        migrationBuilder.DropColumn(name: "ReviewNote", table: "Leads");
        migrationBuilder.DropColumn(name: "Action", table: "LeadHistories");
        migrationBuilder.DropColumn(name: "FromReviewStatus", table: "LeadHistories");
        migrationBuilder.DropColumn(name: "ToReviewStatus", table: "LeadHistories");
        migrationBuilder.DropColumn(name: "AdminId", table: "LeadHistories");
        migrationBuilder.DropColumn(name: "FeaturedImage", table: "SellableProjects");
    }
}