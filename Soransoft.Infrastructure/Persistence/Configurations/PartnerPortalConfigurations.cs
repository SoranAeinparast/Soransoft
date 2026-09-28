using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Soransoft.Domain.Entities;

namespace Soransoft.Infrastructure.Persistence.Configurations
{
    internal sealed class PartnerConfig : IEntityTypeConfiguration<Partner>
    {
        public void Configure(EntityTypeBuilder<Partner> b)
        {
            b.ToTable("Partners");
            b.Property(x => x.Username).HasMaxLength(80).IsRequired();
            b.HasIndex(x => x.Username).IsUnique();
            b.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            b.Property(x => x.Mobile).HasMaxLength(20);
            b.Property(x => x.Email).HasMaxLength(200);
            b.Property(x => x.NationalId).HasMaxLength(20);
            b.Property(x => x.FatherName).HasMaxLength(150);
            b.Property(x => x.BirthCertificateNumber).HasMaxLength(30);
            b.Property(x => x.BirthPlace).HasMaxLength(100);
            b.Property(x => x.Landline).HasMaxLength(30);
            b.Property(x => x.Address).HasColumnType("nvarchar(max)");
            b.Property(x => x.PostalCode).HasMaxLength(20);
            b.Property(x => x.PersonalPhotoPath).HasMaxLength(400);
            b.Property(x => x.NationalCardFrontPath).HasMaxLength(400);
            b.Property(x => x.NationalCardBackPath).HasMaxLength(400);
            b.Property(x => x.BirthCertificatePath).HasMaxLength(400);
            b.Property(x => x.IdentityDocumentPath).HasMaxLength(400);
            b.Property(x => x.TechLevel).HasMaxLength(60);
            b.Property(x => x.Skills).HasMaxLength(500);
            b.Property(x => x.AdminNote).HasColumnType("nvarchar(max)");
        }
    }

    internal sealed class PartnerDocumentConfig : IEntityTypeConfiguration<PartnerDocument>
    {
        public void Configure(EntityTypeBuilder<PartnerDocument> b)
        {
            b.ToTable("PartnerDocuments");
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.StoredPath).HasMaxLength(400).IsRequired();
            b.HasIndex(x => new { x.PartnerId, x.CreatedAt });
            b.HasOne(x => x.Partner).WithMany(x => x.Documents).HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    internal sealed class LeadConfig : IEntityTypeConfiguration<Lead>
    {
        public void Configure(EntityTypeBuilder<Lead> b)
        {
            b.ToTable("Leads");
            b.Property(x => x.FormNo).HasMaxLength(40);
            b.Property(x => x.CustomerCode).HasMaxLength(50);
            b.Property(x => x.SalesPartnerNationalId).HasMaxLength(20);
            b.Property(x => x.SalesPartnerAgreementNo).HasMaxLength(60);
            b.Property(x => x.SalesPartnerBankAccount).HasMaxLength(80);
            b.Property(l => l.EstimatedAmount).HasColumnType("bigint");
            b.Property(l => l.BudgetAmount).HasColumnType("bigint");
            b.Property(x => x.CustomerName).HasMaxLength(150).IsRequired();
            b.Property(x => x.CustomerMobile).HasMaxLength(20).IsRequired();
            b.Property(x => x.BusinessName).HasMaxLength(200);
            b.Property(x => x.DecisionMakerName).HasMaxLength(150);
            b.Property(x => x.DecisionMakerRole).HasMaxLength(120);
            b.Property(x => x.CustomerLandline).HasMaxLength(30);
            b.Property(x => x.CustomerEmail).HasMaxLength(200);
            b.Property(x => x.CurrentWebsite).HasMaxLength(300);
            b.Property(x => x.Province).HasMaxLength(100);
            b.Property(x => x.City).HasMaxLength(100);
            b.Property(x => x.SocialMedia).HasMaxLength(500);
            b.Property(x => x.BusinessType).HasMaxLength(150);
            b.Property(x => x.CurrentSystem).HasMaxLength(150);
            b.Property(x => x.IntroductionMethod).HasMaxLength(200);
            b.Property(x => x.SystemCustomerNo).HasMaxLength(80);
            b.Property(x => x.SecondaryRequirements).HasColumnType("nvarchar(max)");
            b.Property(x => x.ReviewNote).HasColumnType("nvarchar(max)");
            b.Property(x => x.Requirement).HasColumnType("nvarchar(max)");
            b.Property(x => x.AdminNote).HasColumnType("nvarchar(max)");
            b.HasIndex(x => x.Stage);
            b.HasMany(x => x.History).WithOne(x => x.Lead).HasForeignKey(x => x.LeadId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    internal sealed class LeadHistoryConfig : IEntityTypeConfiguration<LeadHistory>
    {
        public void Configure(EntityTypeBuilder<LeadHistory> b)
        {
            b.ToTable("LeadHistories");
            b.Property(x => x.Action).HasMaxLength(40).IsRequired();
            b.Property(x => x.Note).HasMaxLength(500);
            b.HasIndex(x => new { x.LeadId, x.CreatedAt });
        }
    }

    internal sealed class PartnerContractConfig : IEntityTypeConfiguration<PartnerContract>
    {
        public void Configure(EntityTypeBuilder<PartnerContract> b)
        {
            b.ToTable("PartnerContracts");
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.CustomerName).HasMaxLength(150).IsRequired();
            b.Property(x => x.ContractFile).HasMaxLength(300);
            b.Property(x => x.Description).HasColumnType("nvarchar(max)");
            b.HasIndex(x => x.Status);
            b.HasMany(x => x.PaymentStages).WithOne(x => x.Contract).HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.ThirdPartyCosts).WithOne(x => x.Contract).HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.CommissionRates).WithOne(x => x.Contract).HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Tickets).WithOne(x => x.Contract).HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    internal sealed class ContractPaymentStageConfig : IEntityTypeConfiguration<ContractPaymentStage>
    {
        public void Configure(EntityTypeBuilder<ContractPaymentStage> b)
        {
            b.ToTable("ContractPaymentStages");
            b.Property(x => x.Title).HasMaxLength(250).IsRequired();
            b.Property(x => x.Description).HasColumnType("nvarchar(max)");
            b.Property(x => x.PercentOfTotal).HasPrecision(5, 2);
            b.Property(x => x.ReceiptFile).HasMaxLength(300);
            b.Property(x => x.DocumentFile).HasMaxLength(300);
            b.Property(x => x.DocumentNote).HasMaxLength(500);
            b.Property(x => x.ChequeNo).HasMaxLength(60);
            b.Property(x => x.ChequeBank).HasMaxLength(100);
            b.Property(x => x.AdminNote).HasMaxLength(500);
            b.HasIndex(x => new { x.ContractId, x.Number }).IsUnique();
        }
    }

    internal sealed class ThirdPartyCostItemConfig : IEntityTypeConfiguration<ThirdPartyCostItem>
    {
        public void Configure(EntityTypeBuilder<ThirdPartyCostItem> b)
        {
            b.ToTable("ThirdPartyCostItems");
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.TechnicalSpecs).HasColumnType("nvarchar(max)");
            b.Property(x => x.Description).HasColumnType("nvarchar(max)");
            b.HasIndex(x => x.Type);
        }
    }

    internal sealed class ContractThirdPartyCostConfig : IEntityTypeConfiguration<ContractThirdPartyCost>
    {
        public void Configure(EntityTypeBuilder<ContractThirdPartyCost> b)
        {
            b.ToTable("ContractThirdPartyCosts");
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.TechnicalSpecs).HasColumnType("nvarchar(max)");
            b.Property(x => x.Note).HasMaxLength(500);
            b.HasIndex(x => x.ContractId);
            b.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.SetNull);
        }
    }

    internal sealed class CommissionRateConfig : IEntityTypeConfiguration<CommissionRate>
    {
        public void Configure(EntityTypeBuilder<CommissionRate> b)
        {
            b.ToTable("CommissionRates");
            b.Property(r => r.Value).HasPrecision(18, 2);
            b.Property(x => x.TiersJson).HasColumnType("nvarchar(max)");
            b.HasIndex(x => new { x.ContractId, x.PartnerId }).IsUnique();
        }
    }

    internal sealed class SupportTicketConfig : IEntityTypeConfiguration<SupportTicket>
    {
        public void Configure(EntityTypeBuilder<SupportTicket> b)
        {
            b.ToTable("SupportTickets");
            b.Property(x => x.Subject).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasColumnType("nvarchar(max)");
            b.Property(x => x.Response).HasColumnType("nvarchar(max)");
            b.HasIndex(x => x.Status);
        }
    }

    internal sealed class DevProjectConfig : IEntityTypeConfiguration<DevProject>
    {
        public void Configure(EntityTypeBuilder<DevProject> b)
        {
            b.ToTable("DevProjects");
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasColumnType("nvarchar(max)");
            b.Property(x => x.StatusText).HasMaxLength(150);
            b.HasMany(x => x.Tasks).WithOne(x => x.Project).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.SetNull);
            b.HasMany(x => x.Documents).WithOne(x => x.Project).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Members).WithOne(x => x.Project).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.TimeLogs).WithOne(x => x.Project).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.SetNull);
        }
    }

    internal sealed class ProjectMemberConfig : IEntityTypeConfiguration<ProjectMember>
    {
        public void Configure(EntityTypeBuilder<ProjectMember> b)
        {
            b.ToTable("ProjectMembers");
            b.Property(x => x.RoleInProject).HasMaxLength(120);
            b.HasIndex(x => new { x.ProjectId, x.PartnerId }).IsUnique();
        }
    }

    internal sealed class SprintConfig : IEntityTypeConfiguration<Sprint>
    {
        public void Configure(EntityTypeBuilder<Sprint> b)
        {
            b.ToTable("Sprints");
            b.Property(x => x.Title).HasMaxLength(150).IsRequired();
            b.Property(x => x.Goal).HasMaxLength(500);
            b.HasMany(x => x.Tasks).WithOne(x => x.Sprint).HasForeignKey(x => x.SprintId).OnDelete(DeleteBehavior.SetNull);
        }
    }

    internal sealed class DevTaskConfig : IEntityTypeConfiguration<DevTask>
    {
        public void Configure(EntityTypeBuilder<DevTask> b)
        {
            b.ToTable("DevTasks");
            b.Property(x => x.Title).HasMaxLength(250).IsRequired();
            b.Property(x => x.Description).HasColumnType("nvarchar(max)");
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.PartnerId);
        }
    }

    internal sealed class TimeLogConfig : IEntityTypeConfiguration<TimeLog>
    {
        public void Configure(EntityTypeBuilder<TimeLog> b)
        {
            b.ToTable("TimeLogs");
            b.Property(x => x.Note).HasMaxLength(500);
            b.HasIndex(x => new { x.PartnerId, x.WorkDate });
        }
    }

    internal sealed class ProjectDocumentConfig : IEntityTypeConfiguration<ProjectDocument>
    {
        public void Configure(EntityTypeBuilder<ProjectDocument> b)
        {
            b.ToTable("ProjectDocuments");
            b.Property(x => x.Title).HasMaxLength(250).IsRequired();
            b.Property(x => x.Body).HasColumnType("nvarchar(max)");
            b.Property(x => x.Category).HasMaxLength(80).IsRequired();
        }
    }

    internal sealed class SellableProjectConfig : IEntityTypeConfiguration<SellableProject>
    {
        public void Configure(EntityTypeBuilder<SellableProject> b)
        {
            b.ToTable("SellableProjects");
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasColumnType("nvarchar(max)");
            b.Property(x => x.FeaturedImage).HasMaxLength(300);
            b.HasIndex(x => new { x.IsActive, x.DisplayOrder });
            b.HasMany(x => x.Documents).WithOne(x => x.Project).HasForeignKey(x => x.SellableProjectId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Comments).WithOne(x => x.Project).HasForeignKey(x => x.SellableProjectId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.PartnerAccess).WithOne(x => x.Project).HasForeignKey(x => x.SellableProjectId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    internal sealed class SellableProjectDocumentConfig : IEntityTypeConfiguration<SellableProjectDocument>
    {
        public void Configure(EntityTypeBuilder<SellableProjectDocument> b)
        {
            b.ToTable("SellableProjectDocuments");
            b.Property(x => x.Title).HasMaxLength(250).IsRequired();
            b.Property(x => x.Category).HasMaxLength(80).IsRequired();
            b.Property(x => x.StoredPath).HasMaxLength(300);
            b.Property(x => x.ExternalUrl).HasMaxLength(1000);
            b.Property(x => x.OriginalFileName).HasMaxLength(255);
            b.Property(x => x.ContentType).HasMaxLength(150);
            b.HasIndex(x => new { x.SellableProjectId, x.DisplayOrder });
        }
    }

    internal sealed class SellableProjectCommentConfig : IEntityTypeConfiguration<SellableProjectComment>
    {
        public void Configure(EntityTypeBuilder<SellableProjectComment> b)
        {
            b.ToTable("SellableProjectComments");
            b.Property(x => x.Body).HasColumnType("nvarchar(max)").IsRequired();
            b.HasIndex(x => new { x.SellableProjectId, x.CreatedAt });
        }
    }

    internal sealed class SellableProjectPartnerConfig : IEntityTypeConfiguration<SellableProjectPartner>
    {
        public void Configure(EntityTypeBuilder<SellableProjectPartner> b)
        {
            b.ToTable("SellableProjectPartners");
            b.HasIndex(x => new { x.SellableProjectId, x.PartnerId }).IsUnique();
            b.HasOne(x => x.Partner).WithMany().HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    internal sealed class AnnouncementConfig : IEntityTypeConfiguration<Announcement>
    {
        public void Configure(EntityTypeBuilder<Announcement> b)
        {
            b.ToTable("Announcements");
            b.Property(x => x.Title).HasMaxLength(250).IsRequired();
            b.Property(x => x.Body).HasColumnType("nvarchar(max)");
            b.Property(x => x.Audience).HasMaxLength(20).IsRequired();
        }
    }

    internal sealed class PartnerNotificationReadConfig : IEntityTypeConfiguration<PartnerNotificationRead>
    {
        public void Configure(EntityTypeBuilder<PartnerNotificationRead> b)
        {
            b.ToTable("PartnerNotificationReads");
            b.HasIndex(x => new { x.AnnouncementId, x.PartnerId }).IsUnique();
        }
    }

    internal sealed class CooperationAgreementConfig : IEntityTypeConfiguration<CooperationAgreement>
    {
        public void Configure(EntityTypeBuilder<CooperationAgreement> b)
        {
            b.ToTable("CooperationAgreements");
            b.Property(x => x.AgreementNo).HasMaxLength(60).IsRequired();
            b.HasIndex(x => x.AgreementNo).IsUnique();
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Subject).HasColumnType("nvarchar(max)");
            b.Property(x => x.Terms).HasColumnType("nvarchar(max)");
            b.Property(x => x.ContractFile).HasMaxLength(300);
            b.Property(x => x.SettlementTerms).HasColumnType("nvarchar(max)");
            b.Property(x => x.BankName).HasMaxLength(100);
            b.Property(x => x.BankAccountIban).HasMaxLength(34);
            b.Property(x => x.BankAccountHolder).HasMaxLength(150);
            b.Property(x => x.AdminNote).HasColumnType("nvarchar(max)");
            b.Property(x => x.CommissionValue).HasPrecision(18, 2);
            b.HasIndex(x => new { x.PartnerId, x.Kind });
            b.HasMany(x => x.CancellationRequests).WithOne(x => x.Agreement).HasForeignKey(x => x.AgreementId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    internal sealed class AgreementCancellationRequestConfig : IEntityTypeConfiguration<AgreementCancellationRequest>
    {
        public void Configure(EntityTypeBuilder<AgreementCancellationRequest> b)
        {
            b.ToTable("AgreementCancellationRequests");
            b.Property(x => x.Reason).HasColumnType("nvarchar(max)").IsRequired();
            b.Property(x => x.AdminResponse).HasColumnType("nvarchar(max)");
            b.HasIndex(x => new { x.AgreementId, x.Status });
            b.HasIndex(x => new { x.PartnerId, x.Status });
            b.HasOne(x => x.Partner).WithMany().HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.Restrict);
        }
    }

    internal sealed class WalletTransactionConfig : IEntityTypeConfiguration<WalletTransaction>
    {
        public void Configure(EntityTypeBuilder<WalletTransaction> b)
        {
            b.ToTable("WalletTransactions");
            b.Property(x => x.Amount).HasPrecision(18, 2);
            b.Property(x => x.BalanceAfter).HasPrecision(18, 2);
            b.Property(x => x.Description).HasMaxLength(500).IsRequired();
            b.Property(x => x.Reference).HasMaxLength(120);
            b.Property(x => x.DocumentFile).HasMaxLength(300);
            b.Property(x => x.BankTrackingNo).HasMaxLength(80);
            b.HasIndex(x => new { x.PartnerId, x.Status });
            b.HasIndex(x => x.CreatedAt);
            b.HasOne(x => x.Partner).WithMany().HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.Restrict);
        }
    }

    internal sealed class WithdrawalRequestConfig : IEntityTypeConfiguration<WithdrawalRequest>
    {
        public void Configure(EntityTypeBuilder<WithdrawalRequest> b)
        {
            b.ToTable("WithdrawalRequests");
            b.Property(x => x.Amount).HasPrecision(18, 2);
            b.Property(x => x.Note).HasColumnType("nvarchar(max)");
            b.Property(x => x.AdminResponse).HasColumnType("nvarchar(max)");
            b.Property(x => x.DestinationIban).HasMaxLength(34).IsRequired();
            b.Property(x => x.DestinationBank).HasMaxLength(100).IsRequired();
            b.Property(x => x.DestinationHolder).HasMaxLength(150).IsRequired();
            b.HasIndex(x => x.Status);
            b.HasOne(x => x.Partner).WithMany().HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.WalletTransaction).WithMany().HasForeignKey(x => x.WalletTransactionId).OnDelete(DeleteBehavior.SetNull);
        }
    }

    internal sealed class PasswordResetRequestConfig : IEntityTypeConfiguration<PasswordResetRequest>
    {
        public void Configure(EntityTypeBuilder<PasswordResetRequest> b)
        {
            b.ToTable("PasswordResetRequests");
            b.Property(x => x.Message).HasColumnType("nvarchar(max)");
            b.Property(x => x.AdminResponse).HasColumnType("nvarchar(max)");
            b.HasIndex(x => x.Status);
            b.HasOne(x => x.Partner).WithMany().HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
