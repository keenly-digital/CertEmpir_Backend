using CertEmpire.Models.CommonModel;
using System.ComponentModel.DataAnnotations;

namespace CertEmpire.Models
{
    public class UploadedFile : AuditableBaseEntity
    {
        [Key]
        public Guid FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileURL { get; set; } = string.Empty;
        public decimal FilePrice { get; set; }
    }
}