namespace CertEmpire.Services.FileService
{
    public interface IFileService
    {
        Task<string> ExportFileAsync(IFormFile file, string subDirectory);
    }
}
