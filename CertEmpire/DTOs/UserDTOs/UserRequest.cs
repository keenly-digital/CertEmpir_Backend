using CertEmpire.Helpers.Enums;

namespace CertEmpire.DTOs.UserDTOs
{

    #region User DTOs
    public class AddUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public List<LoginFileUploadDTO>? File { get; set; }
    }
    public class LoginFileUploadDTO
    {
        public string? FileUrl { get; set; }
        public decimal FilePrice { get; set; }
        public int ProductId { get; set; }
    }
    public class FileResponse
    {
        public string? FileUrl { get; set; }
    }
    public class UpdatePasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
    #endregion

    #region Admin DTOs
    public class AdminLoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
    public class ChangeEmailAsync
    {
        public string OldEmail { get; set; } = string.Empty;
        public string NewEmail { get; set; } = string.Empty;
    }
    public class ChangePasswordAsync
    {
        public Guid UserId { get; set; }
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
    public class ChangeFirstOrLastName
    {
        public Guid UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
    public class ChangeProfilePic
    {
        public Guid UserId { get; set; }
        public IFormFile Image { get; set; } = null!;
    }
    #endregion
}