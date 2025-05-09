namespace CertEmpire.DTOs.UserDTOs
{
    public class UserResponse
    {
    }
    public class AddUserResponse
    {
        public List<FileResponseObject>? FileObj { get; set; }
    }
    public class FileResponseObject
    {
        public Guid FileId { get; set; }
        public string? FileUrl { get; set; }
    }
    public class GetAllEmailResponse
    {
        public List<string>? Emails { get; set; }
    }
}