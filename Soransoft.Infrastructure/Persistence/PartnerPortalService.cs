using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using Soransoft.Domain.Entities;
using System.Text.Json;
// حرف‌گرفتن ابهام: TaskStatus پرتال جای System.Threading.Tasks.TaskStatus می‌نشیند
using TaskStatus = Soransoft.Domain.Entities.TaskStatus;

namespace Soransoft.Infrastructure.Persistence
{
    /// <summary>سرویس پرتال همکاران — منطق مشترک پنل همکار و پنل ادمین</summary>
    public class PartnerPortalService : IPartnerPortalService
    {
        private readonly SoransoftDbContext _db;
        public PartnerPortalService(SoransoftDbContext db) => _db = db;

        // ============================================================ لیدها

        public async Task<PortalResult> CreateLeadAsync(int partnerId, Lead lead, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(lead.CustomerName) || string.IsNullOrWhiteSpace(lead.CustomerMobile))
                return PortalResult.Fail("نام و شماره مشتری الزامی است.");

            lead.PartnerId = partnerId;
            lead.Stage = LeadStage.New;
            lead.History.Add(new LeadHistory { FromStage = LeadStage.New, ToStage = LeadStage.New, Note = "ثبت اولیه لید" });
            _db.Leads.Add(lead);
            await _db.SaveChangesAsync(ct);
            return PortalResult.Ok("لید با موفقیت ثبت شد.");
        }

        public async Task<PortalResult> UpdateLeadStageAsync(int leadId, LeadStage newStage, string? note, CancellationToken ct = default)
        {
            var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == leadId, ct);
            if (lead is null) return PortalResult.Fail("لید یافت نشد.");
            if (lead.Stage == LeadStage.Contracted)
                return PortalResult.Fail("لید تبدیل‌شده به قرارداد قابل تغییر نیست.");

            var old = lead.Stage;
            if (old == newStage) return PortalResult.Ok("وضعیت تغییری نکرد.");
            lead.Stage = newStage;
            if (newStage == LeadStage.Contracted) lead.ConvertedAt = DateTime.Now;
            lead.History.Add(new LeadHistory { LeadId = lead.Id, FromStage = old, ToStage = newStage, Note = note });
            await _db.SaveChangesAsync(ct);
            return PortalResult.Ok($"وضعیت لید از «{StageTitle(old)}» به «{StageTitle(newStage)}» تغییر کرد.");
        }

        public async Task<PartnerContract?> ConvertLeadToContractAsync(int leadId, long totalAmount, string title, CancellationToken ct = default)
        {
            var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == leadId, ct);
            if (lead is null || lead.Stage == LeadStage.Contracted) return null;

            var contract = new PartnerContract
            {
                Title = string.IsNullOrWhiteSpace(title) ? $"قرارداد {lead.CustomerName}" : title,
                CustomerName = lead.CustomerName,
                TotalAmount = totalAmount,
                Status = ContractStatus.PendingApproval,
                PartnerId = lead.PartnerId,
                LeadId = lead.Id,
            };
            _db.PartnerContracts.Add(contract);

            lead.Stage = LeadStage.Contracted;
            lead.ConvertedAt = DateTime.Now;
            lead.History.Add(new LeadHistory { LeadId = lead.Id, FromStage = LeadStage.Negotiating, ToStage = LeadStage.Contracted, Note = "تبدیل به قرارداد" });
            await _db.SaveChangesAsync(ct);
            return contract;
        }

        // ============================================================ مراحل پرداخت و پورسانت

        /// <summary>تعریف/ویرایش یک مرحله پرداخت — شماره در صورت خالی بودن، بعد از آخرین مرحله قرار می‌گیرد</summary>
        public async Task<PortalResult> SavePaymentStageAsync(ContractPaymentStage stage, bool isNew, CancellationToken ct = default)
        {
            var contract = await _db.PartnerContracts.FirstOrDefaultAsync(c => c.Id == stage.ContractId, ct);
            if (contract is null) return PortalResult.Fail("قرارداد یافت نشد.");
            if (string.IsNullOrWhiteSpace(stage.Title)) return PortalResult.Fail("عنوان مرحله الزامی است.");

            if (isNew)
            {
                if (stage.Number <= 0)
                {
                    var last = await _db.ContractPaymentStages
                        .Where(s => s.ContractId == stage.ContractId)
                        .MaxAsync(s => (int?)s.Number, ct) ?? 0;
                    stage.Number = last + 1;
                }
                _db.ContractPaymentStages.Add(stage);
                await _db.SaveChangesAsync(ct);
                return PortalResult.Ok("مرحله پرداخت افزوده شد.");
            }

            var row = await _db.ContractPaymentStages.FirstOrDefaultAsync(s => s.Id == stage.Id, ct);
            if (row is null) return PortalResult.Fail("مرحله پرداخت یافت نشد.");
            if (row.Status == PaymentStageStatus.Paid)
                return PortalResult.Fail("مرحله پرداخت‌شده قابل ویرایش نیست؛ ابتدا وضعیت آن را برگردانید.");

            if (stage.Number > 0) row.Number = stage.Number;
            row.Title = stage.Title.Trim();
            row.Description = stage.Description;
            row.Amount = stage.Amount;
            row.PercentOfTotal = stage.PercentOfTotal;
            row.DueDate = stage.DueDate;
            row.DocumentNote = stage.DocumentNote;
            row.ChequeNo = stage.ChequeNo;
            row.ChequeBank = stage.ChequeBank;
            row.ChequeDueDate = stage.ChequeDueDate;
            if (!string.IsNullOrWhiteSpace(stage.DocumentFile)) row.DocumentFile = stage.DocumentFile;
            await _db.SaveChangesAsync(ct);
            return PortalResult.Ok("مرحله پرداخت به‌روزرسانی شد.");
        }

        public async Task<PortalResult> DeletePaymentStageAsync(int stageId, CancellationToken ct = default)
        {
            var row = await _db.ContractPaymentStages.FirstOrDefaultAsync(s => s.Id == stageId, ct);
            if (row is null) return PortalResult.Fail("مرحله پرداخت یافت نشد.");
            if (row.Status == PaymentStageStatus.Paid)
                return PortalResult.Fail("مرحله پرداخت‌شده قابل حذف نیست.");
            _db.ContractPaymentStages.Remove(row);
            await _db.SaveChangesAsync(ct);
            return PortalResult.Ok("مرحله پرداخت حذف شد.");
        }

        public async Task<PortalResult> UpdatePaymentStageStatusAsync(int stageId, PaymentStageStatus status, string? adminNote, CancellationToken ct = default)
        {
            var stage = await _db.ContractPaymentStages.FirstOrDefaultAsync(i => i.Id == stageId, ct);
            if (stage is null) return PortalResult.Fail("مرحله پرداخت یافت نشد.");

            stage.Status = status;
            stage.AdminNote = adminNote;
            stage.PaidAt = status == PaymentStageStatus.Paid ? DateTime.Now
                          : status == PaymentStageStatus.Planned ? null : stage.PaidAt;
            if (status == PaymentStageStatus.Paid) stage.DocumentsVerifiedAt ??= DateTime.Now;
            await _db.SaveChangesAsync(ct);
            return PortalResult.Ok("وضعیت مرحله پرداخت به‌روزرسانی شد.");
        }

        /// <summary>مجموع هزینه‌های شخص ثالث کسر‌شونده از این قرارداد</summary>
        public async Task<long> GetThirdPartyCostTotalAsync(int contractId, CancellationToken ct = default)
        {
            var total = await _db.ContractThirdPartyCosts
                .Where(c => c.ContractId == contractId && c.DeductFromFirstPayment)
                .SumAsync(c => (long?)c.Amount, ct);
            return total ?? 0L;
        }

        /// <summary>
        /// مبنای خالص پورسانت: (کل قرارداد یا مجموع مراحل پرداخت‌شده) منهای هزینه‌های شخص ثالث.
        /// قاعده قرارداد: هزینه‌های شخص ثالث از اولین پرداخت مشتری کسر می‌شود و باقی‌مانده مبنای پورسانت است.
        /// </summary>
        public async Task<long> GetCommissionBaseAsync(CommissionRate rate, CancellationToken ct = default)
        {
            long gross;
            if (rate.BasedOnPaidStages)
            {
                gross = await _db.ContractPaymentStages
                    .Where(i => i.ContractId == rate.ContractId && i.Status == PaymentStageStatus.Paid)
                    .SumAsync(i => (long?)i.Amount, ct) ?? 0L;
            }
            else
            {
                gross = await _db.PartnerContracts
                    .Where(c => c.Id == rate.ContractId)
                    .Select(c => (long?)c.TotalAmount)
                    .FirstOrDefaultAsync(ct) ?? 0L;
            }

            var thirdParty = await GetThirdPartyCostTotalAsync(rate.ContractId, ct);
            var net = gross - thirdParty;
            return net < 0 ? 0L : net;
        }

        /// <summary>محاسبه پورسانت — همه روش‌های مرسوم: درصدی ثابت، مبلغ ثابت، پله‌ای (مبنا خالص پس از هزینه شخص ثالث)</summary>
        public async Task<decimal> ComputeCommissionAsync(CommissionRate rate, CancellationToken ct = default)
        {
            decimal baseAmount = await GetCommissionBaseAsync(rate, ct);

            decimal result = rate.Method switch
            {
                // درصد ثابت روی مبلغ مبنا
                CommissionMethod.FixedPercent => Math.Round(baseAmount * rate.Value / 100m, 0),

                // مبلغ ثابت به تومان
                CommissionMethod.FixedAmount => rate.Value,

                // پله‌ای: هر پله درصد خودش را روی بخشی از فروش می‌گیرد
                CommissionMethod.Tiered => ComputeTiered(baseAmount, rate.TiersJson),

                _ => 0m
            };
            return result;
        }

        /// <summary>محاسبه پله‌ای: هر پله درصد خودش را روی بخشی از فروش می‌گیرد</summary>
        private static decimal ComputeTiered(decimal amount, string? tiersJson)
        {
            if (string.IsNullOrWhiteSpace(tiersJson)) return 0m;
            try
            {
                var tiers = JsonSerializer.Deserialize<List<Tier>>(tiersJson);
                if (tiers is null || tiers.Count == 0) return 0m;

                decimal commission = 0m, previous = 0m;
                foreach (var t in tiers.OrderBy(t => t.upTo))
                {
                    var span = (decimal)t.upTo - previous;                     // عرض این پله
                    var inTier = Math.Min(amount, previous + span) - previous; // سهم این پله از فروش
                    if (inTier > 0) commission += inTier * t.percent / 100m;
                    previous += span;
                    if (amount <= previous) break;
                }
                return Math.Round(commission, 0);
            }
            catch (JsonException)
            {
                return 0m;
            }
        }

        private sealed record Tier(long upTo, decimal percent);

        // ============================================================ تسک‌ها

        public async Task<PortalResult> ChangeTaskStatusAsync(int taskId, TaskStatus newStatus, CancellationToken ct = default)
        {
            var task = await _db.DevTasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
            if (task is null) return PortalResult.Fail("تسک یافت نشد.");

            var old = task.Status;
            if (old == newStatus) return PortalResult.Ok("وضعیت تغییری نکرد.");

            task.Status = newStatus;
            task.CompletedAt = newStatus == TaskStatus.Done ? DateTime.Now : null;
            await _db.SaveChangesAsync(ct);
            return PortalResult.Ok($"وضعیت تسک به «{StatusTitle(newStatus)}» تغییر کرد.");
        }

        public static string StageTitle(LeadStage stage) => stage switch
        {
            LeadStage.New => "جدید",
            LeadStage.Negotiating => "در حال مذاکره",
            LeadStage.Contracted => "قرارداد بسته شده",
            LeadStage.Lost => "منصرف شده",
            _ => stage.ToString()
        };

        public static string StatusTitle(TaskStatus status) => status switch
        {
            TaskStatus.ToDo => "انجام نشده",
            TaskStatus.InProgress => "در حال انجام",
            TaskStatus.Testing => "در حال تست",
            TaskStatus.Done => "انجام شده",
            _ => status.ToString()
        };

        // ============================================================ اطلاعیه‌ها

        /// <summary>ارسال اطلاعیه — بازگشت تعداد گیرندگان واقعی بر اساس مخاطب/نقش</summary>
        public async Task<int> SendAnnouncementAsync(Announcement announcement, CancellationToken ct = default)
        {
            IQueryable<Partner> query = _db.Partners.Where(p => !p.IsDeleted && p.IsActive);

            if (announcement.PartnerId is not null)
            {
                // ارسال به یک همکار مشخص
                query = query.Where(p => p.Id == announcement.PartnerId.Value);
            }
            else
            {
                query = announcement.Audience switch
                {
                    "Sales" => query.Where(p => p.Role == PartnerRole.Sales || p.Role == PartnerRole.SalesManager),
                    "Dev" => query.Where(p => p.Role == PartnerRole.Developer || p.Role == PartnerRole.TechManager),
                    _ => query // All
                };
            }

            var recipients = await query.CountAsync(ct);
            _db.Announcements.Add(announcement);
            await _db.SaveChangesAsync(ct);
            return recipients;
        }
    }
}
