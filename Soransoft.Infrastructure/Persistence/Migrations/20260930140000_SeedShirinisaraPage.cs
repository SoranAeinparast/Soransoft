using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Soransoft.Infrastructure.Persistence;

#nullable disable

namespace Soransoft.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoransoftDbContext))]
[Migration("20260930140000_SeedShirinisaraPage")]
public partial class SeedShirinisaraPage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var title = ShirinisaraPageContent.Title.Replace("'", "''");
        var summary = ShirinisaraPageContent.Summary.Replace("'", "''");
        var body = ShirinisaraPageContent.Body.Replace("'", "''");
        var image = ShirinisaraPageContent.Image.Replace("'", "''");
        var seoTitle = ShirinisaraPageContent.SeoTitle.Replace("'", "''");
        var seoDescription = ShirinisaraPageContent.SeoDescription.Replace("'", "''");

        migrationBuilder.Sql($"""
            IF EXISTS (SELECT 1 FROM [SitePages] WHERE [Slug] = N'shirinisara')
            BEGIN
                UPDATE [SitePages]
                SET [Title] = N'{title}',
                    [Summary] = N'{summary}',
                    [Body] = N'{body}',
                    [Image] = N'{image}',
                    [SeoTitle] = N'{seoTitle}',
                    [SeoDescription] = N'{seoDescription}',
                    [DisplayOrder] = 30,
                    [IsPublished] = 1,
                    [UpdatedAt] = GETUTCDATE(),
                    [IsDeleted] = 0,
                    [DeletedAt] = NULL
                WHERE [Slug] = N'shirinisara';
            END
            ELSE
            BEGIN
                INSERT INTO [SitePages]
                    ([Title], [Slug], [Summary], [Body], [Image], [SeoTitle], [SeoDescription], [DisplayOrder], [IsPublished], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
                VALUES
                    (N'{title}', N'shirinisara', N'{summary}', N'{body}', N'{image}', N'{seoTitle}', N'{seoDescription}', 30, 1, GETUTCDATE(), NULL, 0, NULL);
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Content edits made by an administrator must survive a migration rollback.
    }
}
