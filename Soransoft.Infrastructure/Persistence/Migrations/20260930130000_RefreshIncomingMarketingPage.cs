using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Soransoft.Infrastructure.Persistence;

#nullable disable

namespace Soransoft.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoransoftDbContext))]
[Migration("20260930130000_RefreshIncomingMarketingPage")]
public partial class RefreshIncomingMarketingPage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var title = IncomingPageContent.Title.Replace("'", "''");
        var summary = IncomingPageContent.Summary.Replace("'", "''");
        var body = IncomingPageContent.Body.Replace("'", "''");
        var seoTitle = IncomingPageContent.SeoTitle.Replace("'", "''");
        var seoDescription = IncomingPageContent.SeoDescription.Replace("'", "''");

        migrationBuilder.Sql($"""
            IF EXISTS (SELECT 1 FROM [SitePages] WHERE [Slug] = N'incoming')
            BEGIN
                UPDATE [SitePages]
                SET [Title] = N'{title}',
                    [Summary] = N'{summary}',
                    [Body] = N'{body}',
                    [SeoTitle] = N'{seoTitle}',
                    [SeoDescription] = N'{seoDescription}',
                    [DisplayOrder] = 20,
                    [IsPublished] = 1,
                    [UpdatedAt] = GETUTCDATE(),
                    [IsDeleted] = 0,
                    [DeletedAt] = NULL
                WHERE [Slug] = N'incoming';
            END
            ELSE
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
        // Content edits made by an administrator must survive a migration rollback.
    }
}
