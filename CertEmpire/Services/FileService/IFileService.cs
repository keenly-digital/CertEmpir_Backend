using CertEmpire.DTOs.UserDTOs;

namespace CertEmpire.Services.FileService
{
    public interface IFileService
    {
        Task<string> ExportFileAsync(string domain, IFormFile file, string subDirectory);
        Task<string> ChangeProfilePic(ChangeProfilePic request);
    }
}
