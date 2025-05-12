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
            Response<FileReportRewardResponseDTO> response;
            var filePrice = await _context.UploadedFiles.Where(x => x.FileId.Equals(request.FileId)).Select(x => x.FilePrice).FirstOrDefaultAsync();
            if (filePrice == 0)
            {
                response = new Response<FileReportRewardResponseDTO>(true, "No reward on free files.", "", default);
            }
            else
            {
                var approvedReportsCount = await _context.Reports
                    .Where(x => x.fileId.Equals(request.FileId) && x.Status == ReportStatus.Approved)
                    .CountAsync();
                decimal reward = Math.Min(filePrice, approvedReportsCount * 0.33m);
                response = new Response<FileReportRewardResponseDTO>(true, "Rewards", "", new FileReportRewardResponseDTO { FileId = request.FileId, UserId = request.UserId, Reward = reward });
            }
            return response;
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
                        Balance = Math.Min(rg.TotalUnwithdrawn, fileObj.FilePrice)
                    });
                }
            }

            return new Response<object>(true, "User rewards retrieved", "", result);
        }

    }
}
