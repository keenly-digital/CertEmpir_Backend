namespace CertEmpire.Services.FileService
{
    public class FileService : IFileService
    {
        private readonly string _rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
        private readonly IHttpContextAccessor _httpContextAccessor;
        public FileService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }
        public async Task<string> ExportFileAsync(IFormFile file, string subDirectory)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid File");

            string fileExtension = Path.GetExtension(file.FileName).ToLower();
            // Restrict uploads to .qzs files only

            string folderPath = Path.Combine(_rootPath, subDirectory);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string fileName = $"{Guid.NewGuid()}{fileExtension}";
            string filePath = Path.Combine(folderPath, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            var httpRequest = _httpContextAccessor.HttpContext.Request;
            var imageLivePath = string.Concat(httpRequest.Scheme, "://", httpRequest.Host.ToUriComponent(), httpRequest.PathBase.ToUriComponent(), "/uploads/QuizFiles/", fileName);
            return imageLivePath; // Return relative path
        }
    }
}
