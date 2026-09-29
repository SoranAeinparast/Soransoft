using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Soransoft.Domain.Entities;

namespace Soransoft.Infrastructure.Persistence.Configurations
{
    internal sealed class SiteUserConfig : IEntityTypeConfiguration<SiteUser>
    {
        public void Configure(EntityTypeBuilder<SiteUser> b)
        {
            b.ToTable("SiteUsers");
            b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            b.Property(x => x.Mobile).HasMaxLength(20).IsRequired();
            b.Property(x => x.Email).HasMaxLength(200);
            b.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            b.HasIndex(x => x.Mobile).IsUnique();
        }
    }

    internal sealed class ServiceConfig : IEntityTypeConfiguration<Service>
    {
        public void Configure(EntityTypeBuilder<Service> b)
        {
            b.ToTable("Services");
            b.Property(x => x.Title).HasMaxLength(100).IsRequired();
            b.Property(x => x.ShortDescription).HasMaxLength(500);
            b.Property(x => x.FullDescription).HasColumnType("nvarchar(max)");
            b.Property(x => x.Slug).HasMaxLength(120).IsRequired();
            b.Property(x => x.Icon).HasMaxLength(60);
            b.Property(x => x.Image).HasMaxLength(300);
            b.Property(x => x.Slogan).HasMaxLength(200);
            b.HasIndex(x => x.Slug).IsUnique();
            b.HasMany(x => x.Features).WithOne(x => x.Service).HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Steps).WithOne(x => x.Service).HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Portfolios).WithOne(x => x.Service).HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.SetNull);
        }
    }

    internal sealed class ServiceFeatureConfig : IEntityTypeConfiguration<ServiceFeature>
    {
        public void Configure(EntityTypeBuilder<ServiceFeature> b)
        {
            b.ToTable("ServiceFeatures");
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasColumnType("nvarchar(max)");
        }
    }

    internal sealed class ServiceStepConfig : IEntityTypeConfiguration<ServiceStep>
    {
        public void Configure(EntityTypeBuilder<ServiceStep> b)
        {
            b.ToTable("ServiceSteps");
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        }
    }

    internal sealed class PortfolioConfig : IEntityTypeConfiguration<Portfolio>
    {
        public void Configure(EntityTypeBuilder<Portfolio> b)
        {
            b.ToTable("Portfolios");
            b.Property(x => x.Title).HasMaxLength(150).IsRequired();
            b.Property(x => x.Description).HasMaxLength(500).IsRequired();
            b.Property(x => x.Image).HasMaxLength(300);
            b.Property(x => x.Url).HasMaxLength(300);
        }
    }

    internal sealed class ArticleCategoryConfig : IEntityTypeConfiguration<ArticleCategory>
    {
        public void Configure(EntityTypeBuilder<ArticleCategory> b)
        {
            b.ToTable("ArticleCategories");
            b.Property(x => x.Title).HasMaxLength(100).IsRequired();
            b.Property(x => x.Slug).HasMaxLength(120).IsRequired();
            b.HasIndex(x => x.Slug).IsUnique();
        }
    }

    internal sealed class ArticleConfig : IEntityTypeConfiguration<Article>
    {
        public void Configure(EntityTypeBuilder<Article> b)
        {
            b.ToTable("Articles");
            b.Property(x => x.Title).HasMaxLength(250).IsRequired();
            b.Property(x => x.Summary).HasMaxLength(1000);
            b.Property(x => x.Body).HasColumnType("nvarchar(max)");
            b.Property(x => x.Image).HasMaxLength(300);
            b.Property(x => x.AuthorName).HasMaxLength(120).IsRequired();
            b.Property(x => x.Slug).HasMaxLength(280);
            b.HasIndex(x => x.Slug);
            b.HasOne(x => x.ArticleCategory).WithMany(x => x.Articles).HasForeignKey(x => x.ArticleCategoryId).OnDelete(DeleteBehavior.Restrict);
        }
    }

    internal sealed class TariffSectionConfig : IEntityTypeConfiguration<TariffSection>
    {
        public void Configure(EntityTypeBuilder<TariffSection> b)
        {
            b.ToTable("TariffSections");
            b.Property(x => x.Title).HasMaxLength(100).IsRequired();
            b.HasMany(x => x.Packages).WithOne(x => x.TariffSection).HasForeignKey(x => x.TariffSectionId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    internal sealed class TariffPackageConfig : IEntityTypeConfiguration<TariffPackage>
    {
        public void Configure(EntityTypeBuilder<TariffPackage> b)
        {
            b.ToTable("TariffPackages");
            b.Property(x => x.Title).HasMaxLength(150).IsRequired();
            b.Property(x => x.PriceNote).HasMaxLength(100);
            b.Property(x => x.PriceSuffix).HasMaxLength(100);
            b.Property(x => x.Duration).HasMaxLength(100);
        }
    }

    internal sealed class TariffItemConfig : IEntityTypeConfiguration<TariffItem>
    {
        public void Configure(EntityTypeBuilder<TariffItem> b)
        {
            b.ToTable("TariffItems");
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        }
    }

    internal sealed class ProjectOrderConfig : IEntityTypeConfiguration<ProjectOrder>
    {
        public void Configure(EntityTypeBuilder<ProjectOrder> b)
        {
            b.ToTable("ProjectOrders");
            b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            b.Property(x => x.Mobile).HasMaxLength(20).IsRequired();
            b.Property(x => x.Email).HasMaxLength(200);
            b.Property(x => x.Description).HasColumnType("nvarchar(max)");
            b.Property(x => x.AdminNote).HasColumnType("nvarchar(max)");
        }
    }

    internal sealed class ContactMessageConfig : IEntityTypeConfiguration<ContactMessage>
    {
        public void Configure(EntityTypeBuilder<ContactMessage> b)
        {
            b.ToTable("ContactMessages");
            b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            b.Property(x => x.Mobile).HasMaxLength(20).IsRequired();
            b.Property(x => x.Email).HasMaxLength(200);
            b.Property(x => x.Subject).HasMaxLength(200).IsRequired();
            b.Property(x => x.Message).HasColumnType("nvarchar(max)");
            b.Property(x => x.AdminNote).HasColumnType("nvarchar(max)");
            b.Property(x => x.IpAddress).HasMaxLength(50);
        }
    }

    internal sealed class ConsultationRequestConfig : IEntityTypeConfiguration<ConsultationRequest>
    {
        public void Configure(EntityTypeBuilder<ConsultationRequest> b)
        {
            b.ToTable("ConsultationRequests");
            b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            b.Property(x => x.Mobile).HasMaxLength(20).IsRequired();
            b.Property(x => x.Description).HasColumnType("nvarchar(max)");
            b.Property(x => x.AdminNote).HasColumnType("nvarchar(max)");
        }
    }

    internal sealed class TeamMemberConfig : IEntityTypeConfiguration<TeamMember>
    {
        public void Configure(EntityTypeBuilder<TeamMember> b)
        {
            b.ToTable("TeamMembers");
            b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            b.Property(x => x.Role).HasMaxLength(120).IsRequired();
            b.Property(x => x.Image).HasMaxLength(300);
            b.Property(x => x.LinkedinUrl).HasMaxLength(300);
        }
    }

    internal sealed class SliderConfig : IEntityTypeConfiguration<Slider>
    {
        public void Configure(EntityTypeBuilder<Slider> b)
        {
            b.ToTable("Sliders");
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.SubTitle).HasMaxLength(300);
            b.Property(x => x.Image).HasMaxLength(300);
            b.Property(x => x.Link).HasMaxLength(300);
        }
    }

    internal sealed class PromoBannerConfig : IEntityTypeConfiguration<PromoBanner>
    {
        public void Configure(EntityTypeBuilder<PromoBanner> b)
        {
            b.ToTable("PromoBanners");
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Text).HasMaxLength(1000);
            b.Property(x => x.Badge).HasMaxLength(100);
            b.Property(x => x.ButtonText).HasMaxLength(100);
            b.Property(x => x.Image).HasMaxLength(300);
            b.Property(x => x.Link).HasMaxLength(300);
            b.HasIndex(x => new { x.Placement, x.DisplayOrder });
        }
    }

    internal sealed class SiteSettingConfig : IEntityTypeConfiguration<SiteSetting>
    {
        public void Configure(EntityTypeBuilder<SiteSetting> b)
        {
            b.ToTable("SiteSettings");
            b.Property(x => x.Key).HasMaxLength(100).IsRequired();
            b.HasIndex(x => x.Key).IsUnique();
            b.Property(x => x.Value).HasColumnType("nvarchar(max)");
            b.Property(x => x.Title).HasMaxLength(150).IsRequired();
            b.Property(x => x.Group).HasMaxLength(60).IsRequired();
            b.Property(x => x.Type).HasMaxLength(20).IsRequired();
        }
    }

    internal sealed class MenuItemConfig : IEntityTypeConfiguration<MenuItem>
    {
        public void Configure(EntityTypeBuilder<MenuItem> b)
        {
            b.ToTable("MenuItems");
            b.Property(x => x.Title).HasMaxLength(100).IsRequired();
            b.Property(x => x.Url).HasMaxLength(300).IsRequired();
        }
    }

    internal sealed class AdminConfig : IEntityTypeConfiguration<Admin>
    {
        public void Configure(EntityTypeBuilder<Admin> b)
        {
            b.ToTable("Admins");
            b.Property(x => x.Username).HasMaxLength(80).IsRequired();
            b.HasIndex(x => x.Username).IsUnique();
            b.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            b.Property(x => x.FullName).HasMaxLength(150);
            b.Property(x => x.Email).HasMaxLength(200);
        }
    }

    internal sealed class ErrorLogConfig : IEntityTypeConfiguration<ErrorLog>
    {
        public void Configure(EntityTypeBuilder<ErrorLog> b)
        {
            b.ToTable("ErrorLogs");
            b.Property(x => x.Message).HasColumnType("nvarchar(max)").IsRequired();
            b.Property(x => x.StackTrace).HasColumnType("nvarchar(max)");
            b.Property(x => x.Source).HasMaxLength(300);
            b.Property(x => x.Path).HasMaxLength(300);
            b.Property(x => x.HttpMethod).HasMaxLength(10);
            b.Property(x => x.IpAddress).HasMaxLength(50);
            b.Property(x => x.UserName).HasMaxLength(100);
            b.Property(x => x.Severity).HasMaxLength(20);
            b.HasIndex(x => x.CreatedAt);
            b.HasIndex(x => x.Severity);
        }
    }
}
