namespace CertEmpire.DTOs.UserDTOs
{
    public class UserResponse
    {
    }
    public class AddUserResponse
    {
        public List<Guid>? FileId { get; set; }
    }
    public class GetAllEmailResponse
    {
        public List<string>? Emails { get; set; }
    }
}