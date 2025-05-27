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
        public bool Simulation { get; set; } = true;
        public int ProductId { get; set; }
    }
    public class GetAllEmailResponse
    {
        public List<Dictionary<string, string>>? Data { get; set; }
    }
    public class AdminLoginResponse
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string JWToken { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
    }
}