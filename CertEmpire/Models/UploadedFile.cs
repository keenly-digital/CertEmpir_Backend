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
        public int ProductId { get; set; }
        public int OrderId { get; set; }
        public bool Simulation { get; set; } = true;
        public int NumberOfQuestions { get; set; } = 0;
        public Guid UserId { get; set; }
    }
}