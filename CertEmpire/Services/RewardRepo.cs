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
            var filePrice = await _context.UploadedFiles
                .Where(x => x.FileId.Equals(request.FileId))
                .Select(x => x.FilePrice)
                .FirstOrDefaultAsync();

            if (filePrice == 0)
            {
                return new Response<FileReportRewardResponseDTO>(
                    true, "No reward on free files.", "", default);
            }

            var approvedReportsCount = await _context.Reports
                .Where(x => x.fileId.Equals(request.FileId) && x.UserId == request.UserId && x.Status == ReportStatus.Approved)
                .CountAsync();

            decimal rewardAmount = Math.Min(filePrice, approvedReportsCount * 0.33m);

            // Check if reward already exists for this FileId and UserId
            var existingReward = await _context.Rewards
                .FirstOrDefaultAsync(r => r.FileId == request.FileId && r.UserId == request.UserId);

            if (existingReward != null)
            {
                existingReward.Amount = rewardAmount;

                // Detach any tracked instance with same key
                var tracked = _context.ChangeTracker.Entries<Reward>()
                    .FirstOrDefault(e => e.Entity.RewardId == existingReward.RewardId);
                if (tracked != null)
                    tracked.State = EntityState.Detached;

                _context.Rewards.Update(existingReward);
                await _context.SaveChangesAsync();
            }
            else
            {
                Reward newReward = new()
                {
                    ReportId = Guid.NewGuid(),
                    Amount = rewardAmount,
                    FileId = request.FileId,
                    RewardId = Guid.NewGuid(),
                    UserId = request.UserId,
                    Withdrawn = false
                };

                await _context.Rewards.AddAsync(newReward);
                await _context.SaveChangesAsync();
            }

            var responseDto = new FileReportRewardResponseDTO
            {
                FileId = request.FileId,
                UserId = request.UserId,
                CurrentBalance = rewardAmount
            };

            return new Response<FileReportRewardResponseDTO>(true, "Reward calculated.", "", responseDto);
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
            var rewards = await _context.Rewards
                .Where(r => r.UserId == request.UserId && !r.Withdrawn)
                .ToListAsync();
            int pageSize = request.PageNumber * 10;
            //var rewardGroups = rewards
            //    .GroupBy(r => r.FileId)
            //    .Select(g => new
            //    {
            //        FileId = g.Key,
            //        TotalUnwithdrawn = g.Sum(r => r.Amount),
            //        ApprovedReports = g.Count()
            //    }).ToList().Take(pageSize);

            //var fileInfo = await _context.UserFilePrices
            //    .Where(u => u.UserId == request.UserId)
            //    .ToListAsync();
            var reportList = await _context.Reports.Where(x => x.UserId.Equals(request.UserId)).ToListAsync();
            List<object> result = new();

            int orderSeed = 40000;
            int index = 1;
            int totalCount = reportList.Count();
            foreach (var rg in reportList)
            {
                //  var fileRecord = fileInfo.FirstOrDefault(f => f.FileId == rg.FileId);
                var fileObj = await _context.UploadedFiles.FirstOrDefaultAsync(x => x.FileId == rg.fileId);

                if (fileObj != null)
                {
                    var fileId = rg.fileId;

                    var reportsSubmitted = await _context.Reports.CountAsync(x => x.UserId == request.UserId && x.fileId == fileId);
                    var votedReports = await _context.Reports.CountAsync(x => x.UserId == request.UserId && x.fileId == fileId && x.Status == ReportStatus.Voted);

                    // Count of voted reports approved (by the reviewer) — custom logic may be needed here
                    var votedReportsApproved = await _context.ReviewTasks.CountAsync(x =>
                        x.ReviewerUserId == request.UserId &&
                        x.Status == ReportStatus.Voted &&
                        x.VotedStatus == true &&  // You may need to adjust this condition
                        _context.Reports.Any(r => r.ReportId == x.ReportId && r.fileId == fileId));
                    var approvedReports = reportList.Count(x => x.Status.Equals(ReportStatus.Approved));
                    FileReportRewardRequestDTO requestReward = new()
                    {
                        UserId = request.UserId,
                        FileId = fileObj.FileId
                    };
                    var currentBalance = await CalculateReward(requestReward);
                    if (currentBalance.Data!=null)
                    {
                        result.Add(new
                        {
                            OrderNumber = $"#{orderSeed + index++}",
                            FileName = fileObj.FileName,
                            FilePrice = fileObj.FilePrice,
                            ReportsSubmitted = reportsSubmitted,
                            ReportsApproved = approvedReports,
                            VotedReports = votedReports,
                            VotedReportsApproved = votedReportsApproved,
                            CurrentBalance = currentBalance.Data.CurrentBalance
                        });
                    }
                    else
                    {
                        result.Add(new
                        {
                            OrderNumber = $"#{orderSeed + index++}",
                            FileName = fileObj.FileName,
                            FilePrice = fileObj.FilePrice,
                            ReportsSubmitted = reportsSubmitted,
                            ReportsApproved = approvedReports,
                            VotedReports = votedReports,
                            VotedReportsApproved = votedReportsApproved,
                            CurrentBalance = 0
                        });
                    }

                        object obj = new
                        {
                            results = totalCount,
                            data = result,
                        };
                }
            }

            return new Response<object>(true, "Reward details with order retrieved", "", result);
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
