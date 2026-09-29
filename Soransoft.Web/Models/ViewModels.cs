using System.Diagnostics;
using Soransoft.Application.ViewModels;
using Soransoft.Domain.Entities;

namespace Soransoft.Web.Models
{
    public class OurServicesViewModel
    {
        public List<ServiceCardViewModel> Services { get; set; } = new();
        public IDictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();
    }

    public class ArticlesViewModel
    {
        public List<ArticleCategory> Categories { get; set; } = new();
        public List<ArticleCardViewModel> Articles { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int? SelectedCategoryId { get; set; }
        public IDictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();
    }

    public class AboutUsViewModel
    {
        public IDictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();
        public List<TeamMember> Team { get; set; } = new();
    }
}
