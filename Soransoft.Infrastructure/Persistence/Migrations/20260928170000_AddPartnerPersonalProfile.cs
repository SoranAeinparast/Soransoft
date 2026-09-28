using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Soransoft.Infrastructure.Persistence.Migrations;

public partial class AddPartnerPersonalProfile : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "Address", table: "Partners", type: "nvarchar(max)", nullable: true);
        migrationBuilder.AddColumn<string>(name: "BirthCertificateNumber", table: "Partners", type: "nvarchar(30)", maxLength: 30, nullable: true);
        migrationBuilder.AddColumn<string>(name: "BirthCertificatePath", table: "Partners", type: "nvarchar(400)", maxLength: 400, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "BirthDate", table: "Partners", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<string>(name: "BirthPlace", table: "Partners", type: "nvarchar(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<string>(name: "FatherName", table: "Partners", type: "nvarchar(150)", maxLength: 150, nullable: true);
        migrationBuilder.AddColumn<string>(name: "IdentityDocumentPath", table: "Partners", type: "nvarchar(400)", maxLength: 400, nullable: true);
        migrationBuilder.AddColumn<string>(name: "Landline", table: "Partners", type: "nvarchar(30)", maxLength: 30, nullable: true);
        migrationBuilder.AddColumn<string>(name: "NationalCardBackPath", table: "Partners", type: "nvarchar(400)", maxLength: 400, nullable: true);
        migrationBuilder.AddColumn<string>(name: "NationalCardFrontPath", table: "Partners", type: "nvarchar(400)", maxLength: 400, nullable: true);
        migrationBuilder.AddColumn<string>(name: "PersonalPhotoPath", table: "Partners", type: "nvarchar(400)", maxLength: 400, nullable: true);
        migrationBuilder.AddColumn<string>(name: "PostalCode", table: "Partners", type: "nvarchar(20)", maxLength: 20, nullable: true);

        migrationBuilder.CreateTable(
            name: "PartnerDocuments",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                PartnerId = table.Column<int>(type: "int", nullable: false),
                Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                StoredPath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PartnerDocuments", x => x.Id);
                table.ForeignKey("FK_PartnerDocuments_Partners_PartnerId", x => x.PartnerId, "Partners", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_PartnerDocuments_PartnerId_CreatedAt", table: "PartnerDocuments", columns: new[] { "PartnerId", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PartnerDocuments");
        migrationBuilder.DropColumn(name: "Address", table: "Partners");
        migrationBuilder.DropColumn(name: "BirthCertificateNumber", table: "Partners");
        migrationBuilder.DropColumn(name: "BirthCertificatePath", table: "Partners");
        migrationBuilder.DropColumn(name: "BirthDate", table: "Partners");
        migrationBuilder.DropColumn(name: "BirthPlace", table: "Partners");
        migrationBuilder.DropColumn(name: "FatherName", table: "Partners");
        migrationBuilder.DropColumn(name: "IdentityDocumentPath", table: "Partners");
        migrationBuilder.DropColumn(name: "Landline", table: "Partners");
        migrationBuilder.DropColumn(name: "NationalCardBackPath", table: "Partners");
        migrationBuilder.DropColumn(name: "NationalCardFrontPath", table: "Partners");
        migrationBuilder.DropColumn(name: "PersonalPhotoPath", table: "Partners");
        migrationBuilder.DropColumn(name: "PostalCode", table: "Partners");
    }
}
