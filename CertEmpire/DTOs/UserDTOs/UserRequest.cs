namespace CertEmpire.DTOs.UserDTOs
{
    public class UserRequest
    {
    }

    public class AddUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public IEnumerable<FileInfo>? File { get; set; }
    }
    public class FileInfo
    {
        public string? FileUrl { get; set; }
        public double FilePrice { get; set; }
    }
}