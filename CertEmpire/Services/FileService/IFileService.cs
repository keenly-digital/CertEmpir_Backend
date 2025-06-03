using CertEmpire.DTOs.UserDTOs;

namespace CertEmpire.Services.FileService
{
    public interface IFileService
    {
        Task<string> ExportFileAsync(IFormFile file, string subDirectory);
        Task<string> ChangeProfilePic(ChangeProfilePic request);
        Task<string> ExportFileAsync(string domainName, IFormFile file, string subDirectory);
       // Task<string> GenerateFileUrlAsync(string domainName, Guid fileId, string fileName);
    }
}