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
        public decimal CurrentBalance {  get; set; }
        public int ReportsSubmitted { get; set; }
        public int ReportsApproved { get; set; }
        public int VotedReports { get; set; }
        public int VotedReportsApproved { get; set; }
    }
}
