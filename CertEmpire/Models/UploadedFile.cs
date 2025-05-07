using CertEmpire.Models.CommonModel;
using System.ComponentModel.DataAnnotations;

namespace CertEmpire.Models
{
    public class UploadedFile : AuditableBaseEntity
    {
        [Key]
        public Guid FileId { get; set; }
        public Guid UserId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public double FilePrice { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
