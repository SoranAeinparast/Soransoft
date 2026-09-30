using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Soransoft.Infrastructure.Persistence;

#nullable disable

namespace Soransoft.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoransoftDbContext))]
[Migration("20260930150000_RefreshShirinisaraPage")]
public sealed class RefreshShirinisaraPage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var title = ShirinisaraPageContent.Title.Replace("'", "''");
        var summary = ShirinisaraPageContent.Summary.Replace("'", "''");
        var body = ToSqlUnicodeExpression(ShirinisaraPageContent.Body);
        var image = ShirinisaraPageContent.Image.Replace("'", "''");
        var seoTitle = ShirinisaraPageContent.SeoTitle.Replace("'", "''");
        var seoDescription = ShirinisaraPageContent.SeoDescription.Replace("'", "''");

        migrationBuilder.Sql($"""
            IF EXISTS (SELECT 1 FROM [SitePages] WHERE [Slug] = N'shirinisara')
            BEGIN
                UPDATE [SitePages]
                SET [Title] = N'{title}',
                    [Summary] = N'{summary}',
                    [Body] = {body},
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
                    (N'{title}', N'shirinisara', N'{summary}', {body}, N'{image}', N'{seoTitle}', N'{seoDescription}', 30, 1, GETUTCDATE(), NULL, 0, NULL);
            END
            """);
    }

    private static string ToSqlUnicodeExpression(string value)
    {
        const int chunkSize = 3000;
        if (value.Length == 0) return "CAST(N'' AS nvarchar(max))";

        var parts = new List<string>();
        for (var start = 0; start < value.Length;)
        {
            var length = Math.Min(chunkSize, value.Length - start);
            if (start + length < value.Length &&
                char.IsHighSurrogate(value[start + length - 1]) &&
                char.IsLowSurrogate(value[start + length]))
                length--;

            var chunk = value.Substring(start, length).Replace("'", "''");
            parts.Add(parts.Count == 0
                ? $"CAST(N'{chunk}' AS nvarchar(max))"
                : $"N'{chunk}'");
            start += length;
        }

        return string.Join(" + ", parts);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Content edits made by an administrator must survive a migration rollback.
    }
}
