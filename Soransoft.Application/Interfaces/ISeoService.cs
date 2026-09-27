namespace Soransoft.Application.Interfaces
{
    /// <summary>آیتم نقشه سایت برای مقالات</summary>
    public class ArticleSitemapItem
    {
        public int Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public DateTime? LastModified { get; set; }
    }

    /// <summary>آیتم نقشه سایت</summary>
    public class SitemapItem
    {
        public string Url { get; set; } = string.Empty;
        public DateTime? LastModified { get; set; }
        /// <summary>daily / weekly / monthly</summary>
        public string ChangeFrequency { get; set; } = "weekly";
        public double Priority { get; set; } = 0.5;
    }

    /// <summary>سرویس سئو (نقشه سایت)</summary>
    public interface ISeoService
    {
        Task<List<SitemapItem>> GetSitemapAsync(string baseUrl, CancellationToken ct = default);
    }
}
