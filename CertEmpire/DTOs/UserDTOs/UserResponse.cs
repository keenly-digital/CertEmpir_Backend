namespace CertEmpire.DTOs.UserDTOs
{
    public class UserResponse
    {
    }
    public class AddUserResponse
    {
        public List<FileResponseObject>? FileObj { get; set; }
        public string? JwtToken { get; set; }
    }
    public class FileResponseObject
    {
        public Guid FileId { get; set; }
        public string? FileUrl { get; set; }
    }
    public class GetAllEmailResponse
    {
        public List<Dictionary<string, string>>? Data { get; set; }
    }
}