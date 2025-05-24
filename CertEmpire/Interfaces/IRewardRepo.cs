using CertEmpire.DTOs.RewardsDTO;
using CertEmpire.Helpers.ResponseWrapper;

namespace CertEmpire.Interfaces
{
    public interface IRewardRepo
    {
        Task<Response<FileReportRewardResponseDTO>> CalculateReward(FileReportRewardRequestDTO request);
        Task<Response<decimal>> Withdraw(FileReportRewardRequestDTO request);
        Task<Response<object>> GetUserRewardDetailsWithOrder(RewardsFilterDTO request);
        Task<Response<object>> GetCouponCode(GetCouponCodeDTO request);
    }
}