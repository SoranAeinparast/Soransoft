using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Soransoft.Infrastructure.Persistence;

#nullable disable

namespace Soransoft.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoransoftDbContext))]
[Migration("20260929090000_AddPromoBanners")]
public partial class AddPromoBanners : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PromoBanners",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                Badge = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                ButtonText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Image = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                Link = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                Type = table.Column<int>(type: "int", nullable: false),
                Placement = table.Column<int>(type: "int", nullable: false),
                DisplayOrder = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                OpenInNewTab = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PromoBanners", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PromoBanners_Placement_DisplayOrder",
            table: "PromoBanners",
            columns: new[] { "Placement", "DisplayOrder" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PromoBanners");
    }
}
