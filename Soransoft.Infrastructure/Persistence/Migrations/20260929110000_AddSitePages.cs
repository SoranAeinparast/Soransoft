using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Soransoft.Infrastructure.Persistence;

#nullable disable

namespace Soransoft.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoransoftDbContext))]
[Migration("20260929110000_AddSitePages")]
public partial class AddSitePages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SitePages",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Image = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                SeoTitle = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                SeoDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                DisplayOrder = table.Column<int>(type: "int", nullable: false),
                IsPublished = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SitePages", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SitePages_Slug",
            table: "SitePages",
            column: "Slug",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "SitePages");
}
