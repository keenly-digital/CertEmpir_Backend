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

            string tempFolder = Path.Combine(Path.GetTempPath(), "uploads", "ProfilePics");
            Directory.CreateDirectory(tempFolder);

            string fileName = $"{Guid.NewGuid()}{fileExtension}";
            string filePath = Path.Combine(tempFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.Image.CopyToAsync(stream);
            }
            var httpRequest = _httpContextAccessor.HttpContext.Request;
            var imageLivePath = string.Concat(httpRequest.Scheme, "://", httpRequest.Host.ToUriComponent(), httpRequest.PathBase.ToUriComponent(), "/uploads/ProfilePics/", fileName);
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

            string fileName = $"{Guid.NewGuid()}{fileExtension}";
            string filePath = Path.Combine(tempFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            var httpRequest = _httpContextAccessor.HttpContext.Request;
            var imageLivePath = string.Concat(httpRequest.Scheme, "://", httpRequest.Host.ToUriComponent(), httpRequest.PathBase.ToUriComponent(), "/uploads/QuizFiles/", fileName);
            return imageLivePath; // Return relative path
        }
        public async Task<string> GenerateFileUrlAsync(string domainName, Guid fileId, string fileName)
        {
            string tempFolder = Path.Combine(Path.GetTempPath(), "uploads", "QuizFiles");
            Directory.CreateDirectory(tempFolder);

            string fullfileName = $"{fileId}_{fileName}";
            string filePath = Path.Combine(tempFolder, fullfileName??"Quiz File");
            var imageLivePath = string.Concat($"{domainName}/uploads/QuizFiles/", fullfileName);
            return imageLivePath; // Return relative path
        }
    }
}
