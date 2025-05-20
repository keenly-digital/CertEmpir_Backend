namespace CertEmpire.DTOs.UserDTOs
{
    public class AddUserResponse
    {
        public List<FileResponseObject>? FileObj { get; set; }
        public string? JwtToken { get; set; }
    }
    public class FileResponseObject
    {
        public string? FileUrl { get; set; }
    }
    public class LoginResponse
    {
        public Guid UserId { get; set; }
        public string JwtToken { get; set; } = string.Empty;
    }
    public class GetAllEmailResponse
    {
        public List<Dictionary<string, string>>? Data { get; set; }
    }
}