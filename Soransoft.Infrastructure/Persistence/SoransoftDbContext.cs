using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Domain.Abstractions;
using Soransoft.Domain.Entities;
// DbContext برای همه موجودیت‌های پروژه
using System.Linq.Expressions;

namespace Soransoft.Infrastructure.Persistence
{
    /// <summary>DbContext اصلی پروژه</summary>
    public class SoransoftDbContext : DbContext, IUnitOfWork
    {
        public SoransoftDbContext(DbContextOptions<SoransoftDbContext> options) : base(options) { }

        public DbSet<SiteUser> SiteUsers => Set<SiteUser>();
        public DbSet<Service> Services => Set<Service>();
        public DbSet<ServiceFeature> ServiceFeatures => Set<ServiceFeature>();
        public DbSet<ServiceStep> ServiceSteps => Set<ServiceStep>();
        public DbSet<Portfolio> Portfolios => Set<Portfolio>();
        public DbSet<ArticleCategory> ArticleCategories => Set<ArticleCategory>();
        public DbSet<Article> Articles => Set<Article>();
        public DbSet<TariffSection> TariffSections => Set<TariffSection>();
        public DbSet<TariffPackage> TariffPackages => Set<TariffPackage>();
        public DbSet<TariffItem> TariffItems => Set<TariffItem>();
        public DbSet<ProjectOrder> ProjectOrders => Set<ProjectOrder>();
        public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
        public DbSet<ConsultationRequest> ConsultationRequests => Set<ConsultationRequest>();
        public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
        public DbSet<Slider> Sliders => Set<Slider>();
        public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
        public DbSet<MenuItem> MenuItems => Set<MenuItem>();
        public DbSet<Admin> Admins => Set<Admin>();
        public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();

        // ---------- پرتال همکاران ----------
        public DbSet<Partner> Partners => Set<Partner>();
        public DbSet<PartnerDocument> PartnerDocuments => Set<PartnerDocument>();
        public DbSet<Lead> Leads => Set<Lead>();
        public DbSet<LeadHistory> LeadHistories => Set<LeadHistory>();
        public DbSet<PartnerContract> PartnerContracts => Set<PartnerContract>();
        public DbSet<ContractPaymentStage> ContractPaymentStages => Set<ContractPaymentStage>();
        public DbSet<ThirdPartyCostItem> ThirdPartyCostItems => Set<ThirdPartyCostItem>();
        public DbSet<ContractThirdPartyCost> ContractThirdPartyCosts => Set<ContractThirdPartyCost>();
        public DbSet<CommissionRate> CommissionRates => Set<CommissionRate>();
        public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
        public DbSet<DevProject> DevProjects => Set<DevProject>();
        public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
        public DbSet<Sprint> Sprints => Set<Sprint>();
        public DbSet<DevTask> DevTasks => Set<DevTask>();
        public DbSet<TimeLog> TimeLogs => Set<TimeLog>();
        public DbSet<ProjectDocument> ProjectDocuments => Set<ProjectDocument>();
        public DbSet<SellableProject> SellableProjects => Set<SellableProject>();
        public DbSet<SellableProjectDocument> SellableProjectDocuments => Set<SellableProjectDocument>();
        public DbSet<SellableProjectComment> SellableProjectComments => Set<SellableProjectComment>();
        public DbSet<SellableProjectPartner> SellableProjectPartners => Set<SellableProjectPartner>();
        public DbSet<Announcement> Announcements => Set<Announcement>();
        public DbSet<PartnerNotificationRead> PartnerNotificationReads => Set<PartnerNotificationRead>();
        public DbSet<CooperationAgreement> CooperationAgreements => Set<CooperationAgreement>();
        public DbSet<AgreementCancellationRequest> AgreementCancellationRequests => Set<AgreementCancellationRequest>();
        public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
        public DbSet<WithdrawalRequest> WithdrawalRequests => Set<WithdrawalRequest>();
        public DbSet<PasswordResetRequest> PasswordResetRequests => Set<PasswordResetRequest>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SoransoftDbContext).Assembly);

            // فیلتر جهانی حذف نرم
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseDeletableEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var body = Expression.Equal(
                        Expression.Property(parameter, nameof(BaseDeletableEntity.IsDeleted)),
                        Expression.Constant(false));
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(body, parameter));
                }
            }

            // فیلترهای متناظر روی وابستگان برای جلوگیری از هشدار EF
            modelBuilder.Entity<ServiceFeature>().HasQueryFilter(f => !f.Service!.IsDeleted);
            modelBuilder.Entity<ServiceStep>().HasQueryFilter(s => !s.Service!.IsDeleted);
            modelBuilder.Entity<TariffItem>().HasQueryFilter(i => !i.TariffPackage!.IsDeleted);

            // متناظرهای پرتال همکاران
            modelBuilder.Entity<LeadHistory>().HasQueryFilter(h => !h.Lead!.IsDeleted);
            modelBuilder.Entity<ContractPaymentStage>().HasQueryFilter(i => !i.Contract!.IsDeleted);
            modelBuilder.Entity<ContractThirdPartyCost>().HasQueryFilter(c => !c.Contract!.IsDeleted);
            modelBuilder.Entity<CommissionRate>().HasQueryFilter(r => !r.Contract!.IsDeleted && !r.Partner!.IsDeleted);
            modelBuilder.Entity<SupportTicket>().HasQueryFilter(t => !t.Contract!.IsDeleted && !t.Partner!.IsDeleted);
            modelBuilder.Entity<ProjectMember>().HasQueryFilter(m => !m.Project!.IsDeleted && !m.Partner!.IsDeleted);
            modelBuilder.Entity<TimeLog>().HasQueryFilter(t => !t.Partner!.IsDeleted);
            modelBuilder.Entity<PartnerNotificationRead>().HasQueryFilter(r => !r.Announcement!.IsDeleted && !r.Partner!.IsDeleted);
            modelBuilder.Entity<CooperationAgreement>().HasQueryFilter(a => !a.Partner!.IsDeleted);
            modelBuilder.Entity<AgreementCancellationRequest>().HasQueryFilter(r => !r.Partner!.IsDeleted && !r.Agreement!.IsDeleted);
            modelBuilder.Entity<WalletTransaction>().HasQueryFilter(t => !t.Partner!.IsDeleted);
            modelBuilder.Entity<WithdrawalRequest>().HasQueryFilter(r => !r.Partner!.IsDeleted);
            modelBuilder.Entity<PasswordResetRequest>().HasQueryFilter(r => !r.Partner!.IsDeleted);
            modelBuilder.Entity<SellableProjectDocument>().HasQueryFilter(d => !d.Project!.IsDeleted);
            modelBuilder.Entity<SellableProjectComment>().HasQueryFilter(c => !c.Project!.IsDeleted);
            modelBuilder.Entity<SellableProjectPartner>().HasQueryFilter(a => !a.Project!.IsDeleted && !a.Partner!.IsDeleted);
        }

        /// <summary>ثبت خودکار تاریخ تغییر</summary>
        public override int SaveChanges()
        {
            TouchTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            TouchTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void TouchTimestamps()
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.Now;
                        break;
                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.Now;
                        break;
                }
            }
        }
    }
}
