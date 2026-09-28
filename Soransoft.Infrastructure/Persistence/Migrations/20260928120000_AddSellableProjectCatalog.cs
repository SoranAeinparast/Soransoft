using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Soransoft.Infrastructure.Persistence;

#nullable disable

namespace Soransoft.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoransoftDbContext))]
[Migration("20260928120000_AddSellableProjectCatalog")]
public partial class AddSellableProjectCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SellableProjects",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                DisplayOrder = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_SellableProjects", x => x.Id));

        migrationBuilder.CreateTable(
            name: "SellableProjectComments",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                SellableProjectId = table.Column<int>(type: "int", nullable: false),
                Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SellableProjectComments", x => x.Id);
                table.ForeignKey("FK_SellableProjectComments_SellableProjects_SellableProjectId", x => x.SellableProjectId, "SellableProjects", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SellableProjectDocuments",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                SellableProjectId = table.Column<int>(type: "int", nullable: false),
                Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                Kind = table.Column<int>(type: "int", nullable: false),
                StoredPath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                ExternalUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                DisplayOrder = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SellableProjectDocuments", x => x.Id);
                table.ForeignKey("FK_SellableProjectDocuments_SellableProjects_SellableProjectId", x => x.SellableProjectId, "SellableProjects", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SellableProjectPartners",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                SellableProjectId = table.Column<int>(type: "int", nullable: false),
                PartnerId = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SellableProjectPartners", x => x.Id);
                table.ForeignKey("FK_SellableProjectPartners_Partners_PartnerId", x => x.PartnerId, "Partners", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_SellableProjectPartners_SellableProjects_SellableProjectId", x => x.SellableProjectId, "SellableProjects", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_SellableProjects_IsActive_DisplayOrder", table: "SellableProjects", columns: new[] { "IsActive", "DisplayOrder" });
        migrationBuilder.CreateIndex(name: "IX_SellableProjectComments_SellableProjectId_CreatedAt", table: "SellableProjectComments", columns: new[] { "SellableProjectId", "CreatedAt" });
        migrationBuilder.CreateIndex(name: "IX_SellableProjectDocuments_SellableProjectId_DisplayOrder", table: "SellableProjectDocuments", columns: new[] { "SellableProjectId", "DisplayOrder" });
        migrationBuilder.CreateIndex(name: "IX_SellableProjectPartners_PartnerId", table: "SellableProjectPartners", column: "PartnerId");
        migrationBuilder.CreateIndex(name: "IX_SellableProjectPartners_SellableProjectId_PartnerId", table: "SellableProjectPartners", columns: new[] { "SellableProjectId", "PartnerId" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SellableProjectComments");
        migrationBuilder.DropTable(name: "SellableProjectDocuments");
        migrationBuilder.DropTable(name: "SellableProjectPartners");
        migrationBuilder.DropTable(name: "SellableProjects");
    }
}
