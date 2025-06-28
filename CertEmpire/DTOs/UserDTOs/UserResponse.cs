using CertEmpire.DTOs.UserRoleDTOs;

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
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string JWToken { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string ProfilePicUrl { get; set; } = string.Empty;
        public Permissions? Permissions { get; set; } = new Permissions();
    }
    public class AddNewUserResponse
    {
        public Guid UserId { get; set; } = Guid.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public Guid UserRoleId { get; set; }
        public string UserName { get; set; } = string.Empty;
    }
    public class GetAllUsersResponse
    {
        public Guid UserId { get; set; } = Guid.Empty;
        public string Name { get; set; } = string.Empty;
        public string ProfilePicUrl { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Role { get; set; } = string.Empty;
    }
    public class EditUser
    {
        public Guid UserId { get; set; } = Guid.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }
}