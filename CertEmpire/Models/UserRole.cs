using CertEmpire.Models.CommonModel;

namespace CertEmpire.Models
{
    public class UserRole : AuditableBaseEntity
    {
        public Guid UserRoleId {  get; set; }
        public string UserRoleName { get; set; } = string.Empty;
        public bool FileCreation {  get; set; }
        public bool Tasks { get; set; }
        public bool UserManagement {  get; set; }
        public bool Edit {  get; set; }
        public bool Delete { get; set; }
        public bool Create { get; set; }
    }
}