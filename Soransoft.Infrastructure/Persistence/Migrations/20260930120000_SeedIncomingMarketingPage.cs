using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Soransoft.Infrastructure.Persistence;

#nullable disable

namespace Soransoft.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoransoftDbContext))]
[Migration("20260930120000_SeedIncomingMarketingPage")]
public partial class SeedIncomingMarketingPage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var title = IncomingPageContent.Title.Replace("'", "''");
        var summary = IncomingPageContent.Summary.Replace("'", "''");
        var body = IncomingPageContent.Body.Replace("'", "''");
        var seoTitle = IncomingPageContent.SeoTitle.Replace("'", "''");
        var seoDescription = IncomingPageContent.SeoDescription.Replace("'", "''");

        migrationBuilder.Sql($"""
            IF NOT EXISTS (SELECT 1 FROM [SitePages] WHERE [Slug] = N'incoming')
            BEGIN
                INSERT INTO [SitePages]
                    ([Title], [Slug], [Summary], [Body], [Image], [SeoTitle], [SeoDescription], [DisplayOrder], [IsPublished], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
                VALUES
                    (N'{title}', N'incoming', N'{summary}', N'{body}', N'', N'{seoTitle}', N'{seoDescription}', 20, 1, GETUTCDATE(), NULL, 0, NULL);
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM [SitePages] WHERE [Slug] = N'incoming' AND [Body] LIKE N'%soransoft-incoming-v1%'");
    }
}
