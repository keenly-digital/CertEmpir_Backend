using CertEmpire.DTOs.UserDTOs;

namespace CertEmpire.Services.FileService
{
    public class FileService : IFileService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public FileService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public async Task<string> ChangeProfilePic(ChangeProfilePic request)
        {
            if (request.Image == null || request.Image.Length == 0)
                throw new ArgumentException("Invalid File");

            string fileExtension = Path.GetExtension(request.Image.FileName).ToLower();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            // Restrict uploads to .qzs files only

            string folderPath = Path.Combine(Path.GetTempPath(), "uploads", "ProfilePics");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string fileName = $"{Guid.NewGuid()}{fileExtension}";
            string filePath = Path.Combine(folderPath, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.Image.CopyToAsync(stream);
            }
            var httpRequest = _httpContextAccessor.HttpContext.Request;
            var imageLivePath = string.Concat(httpRequest.Scheme, "://", httpRequest.Host.ToUriComponent(), httpRequest.PathBase.ToUriComponent(), "/uploads/ProfilePics/", fileName);
            return imageLivePath; // Return relative path
        }

        public async Task<string> ExportFileAsync(string domain, IFormFile file, string subDirectory)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid File");

            string fileExtension = Path.GetExtension(file.FileName).ToLower();
            // Restrict uploads to .qzs files only

            string folderPath = Path.Combine(Path.GetTempPath(), "uploads", "QuizFiles");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string fileName = $"{Guid.NewGuid()}{fileExtension}";
            string filePath = Path.Combine(folderPath, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
           // var httpRequest = _httpContextAccessor.HttpContext.Request;
            var imageLivePath = string.Concat($"{domain}/uploads/QuizFiles/", fileName);
            return imageLivePath; // Return relative path
        }
    }
}
