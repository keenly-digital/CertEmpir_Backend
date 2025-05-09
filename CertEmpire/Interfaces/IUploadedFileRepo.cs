using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Models;

namespace CertEmpire.Interfaces
{
    public interface IUploadedFileRepo : IRepository<UploadedFile>
    {
        Task<Response<UploadedFile>> GetFileById(Guid fileId);
        Task<Response<UploadedFile>> GetFileByFileUrl(string fileUrl);
    }
}