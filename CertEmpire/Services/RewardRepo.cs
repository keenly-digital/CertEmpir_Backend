using CertEmpire.Data;
using CertEmpire.DTOs.RewardsDTO;
using CertEmpire.Helpers.Enums;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Models;
using Microsoft.AspNetCore.Mvc;
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
                .Where(x => x.fileId.Equals(request.FileId) && x.Status == ReportStatus.Approved)
                .CountAsync();

            decimal rewardAmount = Math.Min(filePrice, approvedReportsCount * 0.33m);

            // Check if reward already exists for this FileId and UserId
            var existingReward = await _context.Rewards
                .FirstOrDefaultAsync(r => r.FileId == request.FileId && r.UserId == request.UserId);

            if (existingReward != null)
            {
                // Update the existing reward amount
                existingReward.Amount = rewardAmount;
                _context.Rewards.Update(existingReward);
            }
            else
            {
                // Add a new reward
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
            }

            await _context.SaveChangesAsync();

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
        public async Task<Response<object>> GetUserRewards(Guid userId)
        {
            var rewardGroups = await _context.Rewards
                .Where(r => r.UserId == userId && !r.Withdrawn)
                .GroupBy(r => r.FileId)
                .Select(g => new
                {
                    FileId = g.Key,
                    TotalUnwithdrawn = g.Sum(r => r.Amount),
                    ApprovedReports = g.Count()
                }).ToListAsync();

            var fileInfo = await _context.UserFilePrices
                .Where(u => u.UserId == userId)
                .ToListAsync();

            List<object> result = new();

            foreach (var rg in rewardGroups)
            {
                var fileRecord = fileInfo.FirstOrDefault(f => f.FileId == rg.FileId);
                var fileObj = await _context.UploadedFiles.FirstOrDefaultAsync(x => x.FileId == rg.FileId);

                if (fileRecord != null && fileObj != null)
                {
                    result.Add(new
                    {
                        FileId = rg.FileId,
                        FileName = fileObj.FileName,
                        FilePrice = fileObj.FilePrice,
                        ApprovedReports = rg.ApprovedReports,
                        ReportsSubmitted = await _context.Reports.CountAsync(x => x.UserId == userId && x.fileId == rg.FileId),
                        VotedReports = await _context.Reports.CountAsync(x => x.UserId == userId && x.fileId == rg.FileId && x.Status == ReportStatus.Voted),
                      //  VotedReportsApproved = await _context.Reports.CountAsync(x => x.UserId == userId && x.fileId == rg.FileId && x.Status == ReportStatus.Voted && x.VoteStatus == ReportStatus.Approved),
                        VotedReportsApproved = 0,
                        Balance = Math.Min(rg.TotalUnwithdrawn, fileObj.FilePrice)
                    });
                }
            }

            return new Response<object>(true, "User rewards retrieved", "", result);
        }

    }
}
