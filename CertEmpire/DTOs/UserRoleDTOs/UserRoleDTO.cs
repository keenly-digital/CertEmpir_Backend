namespace CertEmpire.DTOs.UserRoleDTOs
{
    public class AddUserRoleRequest
    {
        public string UserRoleName { get; set; } = string.Empty;
        public bool FileCreation { get; set; }
        public bool Tasks { get; set; }
        public bool UserManagement { get; set; }
        public bool Edit { get; set; }
        public bool Delete { get; set; }
        public bool Create { get; set; }
    }
    public class AddUserRoleResponse
    {
        public Guid UserRoleId {  get; set; }
        public string UserRoleName { get; set; } = string.Empty;
        public bool FileCreation { get; set; }
        public bool Tasks { get; set; }
        public bool UserManagement { get; set; }
        public bool Edit { get; set; }
        public bool Delete { get; set; }
        public bool Create { get; set; }
    }
    public class Permissions
    {
        public bool FileCreation { get; set; }
        public bool Tasks { get; set; }
        public bool UserManagement { get; set; }
        public bool Edit { get; set; }
        public bool Delete { get; set; }
        public bool Create { get; set; }
    }
    public class EditUserRoleResponse
    {
        public Guid UserRoleId { get; set; }
        public string UserRoleName { get; set; } = string.Empty;
        public bool? FileCreation { get; set; }
        public bool? Tasks { get; set; }
        public bool? UserManagement { get; set; }
        public bool? Edit { get; set; }
        public bool? Delete { get; set; }
        public bool? Create { get; set; }
    }
}