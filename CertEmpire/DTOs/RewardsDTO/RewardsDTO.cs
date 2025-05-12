namespace CertEmpire.DTOs.RewardsDTO
{
    public class FileReportRewardRequestDTO
    {
        public Guid UserId {  get; set; }
        public Guid FileId {  get; set; }
    }
    public class FileReportRewardResponseDTO
    {
        public Guid UserId { get; set; }
        public Guid FileId {  get; set; }
        public decimal Reward {  get; set; }
    }
}
