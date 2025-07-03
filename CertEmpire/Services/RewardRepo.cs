using CertEmpire.Data;
using CertEmpire.DTOs.RewardsDTO;
using CertEmpire.Helpers.Enums;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Models;
using Microsoft.EntityFrameworkCore;

namespace CertEmpire.Services
{
    public class RewardRepo(ApplicationDbContext context) : Repository<Reward>(context), IRewardRepo
    {
        public async Task<Response<FileReportRewardResponseDTO>> CalculateReward(FileReportRewardRequestDTO request)
        {
            var filePrice = await _context.UserFilePrices
                .Where(x => x.FileId == request.FileId && x.UserId == request.UserId)
                .Select(x => x.FilePrice)
                .FirstOrDefaultAsync();

            if (filePrice == 0)
            {
                return new Response<FileReportRewardResponseDTO>(true, "No reward on free files.", "", default);
            }

            var approvedReportsCount = await _context.Reports
                .Where(x => x.fileId == request.FileId && x.UserId == request.UserId && x.Status == ReportStatus.Approved)
                .CountAsync();

            decimal rewardAmount = Math.Min(filePrice, approvedReportsCount * 0.33m);

            // ✅ 1. Detach if already tracked
            var tracked = _context.ChangeTracker.Entries<Reward>()
                .FirstOrDefault(e => e.Entity.UserId == request.UserId && e.Entity.FileId == request.FileId);
            if (tracked != null)
                tracked.State = EntityState.Detached;

            // ✅ 2. Safely get the existing reward (no tracking)
            var existingReward = await _context.Rewards
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.FileId == request.FileId && r.UserId == request.UserId);

            if (existingReward != null)
            {
                existingReward.Amount = rewardAmount;
                _context.Rewards.Update(existingReward);
            }
            else
            {
                Reward newReward = new()
                {
                    RewardId = Guid.NewGuid(),
                    ReportId = Guid.NewGuid(),
                    Amount = rewardAmount,
                    FileId = request.FileId,
                    UserId = request.UserId,
                    Withdrawn = false
                };
                await _context.Rewards.AddAsync(newReward);
            }

            await _context.SaveChangesAsync();

            return new Response<FileReportRewardResponseDTO>(true, "Reward calculated.", "", new FileReportRewardResponseDTO
            {
                FileId = request.FileId,
                UserId = request.UserId,
                CurrentBalance = rewardAmount
            });
        }
        public async Task<Response<decimal>> Withdraw(FileReportRewardRequestDTO request)
        {
            Response<decimal> response;
            var filePrice = await _context.UploadedFiles.Where(x => x.FileId.Equals(request.FileId)).Select(x => x.FilePrice).FirstOrDefaultAsync();
            if (filePrice == 0)
            {
                response = new Response<decimal>(true, "No reward on free files.", "", default);
            }
            else
            {
                var existingWithdrawal = await _context.Withdrawals.AnyAsync(x => x.UserId == request.UserId && x.FileId == request.FileId);
                if (existingWithdrawal)
                    return new Response<decimal>(true, "Reward already withdrawn for this file.", "", default);
                var approvedReportsCount = await _context.Reports.CountAsync(x => x.UserId == request.UserId && x.fileId == request.FileId && x.Status == ReportStatus.Approved);
                decimal reward = Math.Min(filePrice, approvedReportsCount * 0.33m);
                var withdrawal = new Withdrawal
                {
                    WithdrawalId = Guid.NewGuid(),
                    UserId = request.UserId,
                    FileId = request.FileId,
                    Amount = reward,
                    Date = DateTime.UtcNow
                };
                await _context.Withdrawals.AddAsync(withdrawal);
                await _context.SaveChangesAsync();
                response = new Response<decimal>(true, "Rewards", "", reward);
            }
            return response;
        }
        public async Task<Response<object>> GetUserRewardDetailsWithOrder(RewardsFilterDTO request)
        {
            var reportList = await _context.Reports
                .Where(r => r.UserId == request.UserId)
                .ToListAsync();

            // Group reports by file to avoid duplicates
            var groupedReports = reportList
                .GroupBy(r => r.fileId)
                .Select(g => new
                {
                    FileId = g.Key,
                    Reports = g.ToList()
                }).ToList();

            List<object> result = new();
            int totalCount = groupedReports.Count;

            foreach (var group in groupedReports)
            {
                var fileId = group.FileId;

                var userFileInfo = await _context.UserFilePrices
                    .FirstOrDefaultAsync(x => x.UserId == request.UserId && x.FileId == fileId);

                // Skip if file price is not set
                if (userFileInfo == null || userFileInfo.FilePrice == 0)
                    continue;

                var fileObj = await _context.UploadedFiles
                    .FirstOrDefaultAsync(x => x.FileId == fileId);

                if (fileObj == null)
                    continue;

                var reportsSubmitted = group.Reports.Count;
                var reportsApproved = group.Reports.Count(x => x.Status == ReportStatus.Approved);
                var votedReports = group.Reports.Count(x => x.Status == ReportStatus.Voted);

                var votedReportsApproved = await _context.ReviewTasks.CountAsync(x =>
                    x.ReviewerUserId == request.UserId &&
                    x.Status == ReportStatus.Voted &&
                    x.VotedStatus == true &&
                    _context.Reports.Any(r => r.ReportId == x.ReportId && r.fileId == fileId));

                var currentBalanceResponse = await CalculateReward(new FileReportRewardRequestDTO
                {
                    UserId = request.UserId,
                    FileId = fileId
                });

                var currentBalance = currentBalanceResponse.Data?.CurrentBalance ?? 0;

                result.Add(new
                {
                    OrderNumber = userFileInfo.OrderId,
                    FileName = fileObj.FileName,
                    FilePrice = fileObj.FilePrice,
                    ReportsSubmitted = reportsSubmitted,
                    ReportsApproved = reportsApproved,
                    VotedReports = votedReports,
                    VotedReportsApproved = votedReportsApproved,
                    CurrentBalance = currentBalance
                });
            }

            return new Response<object>(
                true,
                "Reward details with order retrieved",
                "",
                new
                {
                    results = totalCount,
                    data = result
                });
        }

        public async Task<Response<object>> GetCouponCode(GetCouponCodeDTO request)
        {
            var filePrice = await _context.UploadedFiles
                .Where(x => x.FileId == request.FileId)
                .Select(x => x.FilePrice)
                .FirstOrDefaultAsync();

            if (filePrice == 0)
                return new Response<object>(true, "No reward on free files.", "", null);

            var alreadyWithdrawn = await _context.Withdrawals
                .AnyAsync(x => x.UserId == request.UserId && x.FileId == request.FileId);

            if (alreadyWithdrawn)
                return new Response<object>(true, "Reward already withdrawn for this file.", "", null);

            var approvedReportsCount = await _context.Reports
                .CountAsync(x => x.UserId == request.UserId && x.fileId == request.FileId && x.Status == ReportStatus.Approved);

            decimal reward = Math.Min(filePrice, approvedReportsCount * 0.33m);

            var withdrawal = new Withdrawal
            {
                WithdrawalId = Guid.NewGuid(),
                UserId = request.UserId,
                FileId = request.FileId,
                Amount = reward,
                Date = DateTime.UtcNow,
                Method = WithdrawalMethod.Coupon, // e.g. enum value
                CouponCode = null // to be assigned manually
            };

            await _context.Withdrawals.AddAsync(withdrawal);
            await _context.SaveChangesAsync();

            return new Response<object>(true, "A coupon will be sent to your email shortly.", "", new
            {
                Amount = reward,
                Method = "Coupon"
            });
        }
    }
}
