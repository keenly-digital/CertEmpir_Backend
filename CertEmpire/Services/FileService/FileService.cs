using CertEmpire.DTOs.UserDTOs;

namespace CertEmpire.Services.FileService
{
    public class FileService : IFileService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _env;
        public FileService(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env)
        {
            {
                _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
                _env = env;
            }
        }

        public async Task<string> ChangeProfilePic(ChangeProfilePic request)
        {
            if (request.Image == null || request.Image.Length == 0)
                throw new ArgumentException("Invalid File");

            string fileExtension = Path.GetExtension(request.Image.FileName).ToLower();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            // Restrict uploads to .qzs files only

            string tempFolder = Path.Combine(Path.GetTempPath(), "uploads", "ProfilePics");
            Directory.CreateDirectory(tempFolder);

            string fileName = $"{Guid.NewGuid()}{fileExtension}";
            string filePath = Path.Combine(tempFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.Image.CopyToAsync(stream);
            }
            var httpRequest = _httpContextAccessor.HttpContext.Request;
            var imageLivePath = string.Concat($"https://", httpRequest.Host.ToUriComponent(), httpRequest.PathBase.ToUriComponent(), "/uploads/ProfilePics/", fileName);
            return imageLivePath; // Return relative path
        }

        public async Task<string> ExportFileAsync(IFormFile file, string subDirectory)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid File");

            string fileExtension = Path.GetExtension(file.FileName).ToLower();
            // Restrict uploads to .qzs files only

            string tempFolder = Path.Combine(Path.GetTempPath(), "uploads", "QuizFiles");
            Directory.CreateDirectory(tempFolder);

            string fileName = $"{file.FileName}";
            string filePath = Path.Combine(tempFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            var httpRequest = _httpContextAccessor.HttpContext.Request;
            var imageLivePath = string.Concat("https://", httpRequest.Host.ToUriComponent(), httpRequest.PathBase.ToUriComponent(), "/uploads/QuizFiles/", fileName);
            return imageLivePath; // Return relative path
        }
        public async Task<string> ExportFileAsync(string domainName, IFormFile file, string subDirectory)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file");

            string tempFolder = Path.Combine(Path.GetTempPath(), "uploads", subDirectory);
            Directory.CreateDirectory(tempFolder);

            string originalName = Path.GetFileName(file.FileName);
            string filePath = Path.Combine(tempFolder, originalName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // URL-encode filename
            string safeFileName = Uri.EscapeDataString(originalName);
            string fileUrl = $"{domainName}/uploads/{subDirectory}/{safeFileName}";

            return fileUrl;
        }

    }
}
