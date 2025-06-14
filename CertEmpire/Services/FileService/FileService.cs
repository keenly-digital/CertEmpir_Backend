using CertEmpire.DTOs.UserDTOs;
using Supabase.Storage;

namespace CertEmpire.Services.FileService
{
    public class FileService : IFileService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _env;
        private readonly Supabase.Client _supabaseClient;
        public FileService(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env, Supabase.Client supabaseClient)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _env = env;
            _supabaseClient = supabaseClient;
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
            string fileName = $"{file.FileName}";
            //Read file into memory stream
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0; // Reset stream position
            //Upload to Supabase Storage
            var storage = _supabaseClient.Storage.From("quizfiles");
            var fileBytes = memoryStream.ToArray();
            var result = await storage.Upload(fileBytes, fileName, new Supabase.Storage.FileOptions
            {
                Upsert = true,
                ContentType = file.ContentType ?? "application/octet-stream"
            });
            if (result==null)
                throw new Exception($"Failed to upload file to Supabase. Status: {400}");

            // Get public URL
            var publicUrl = storage.GetPublicUrl(fileName);
            return publicUrl;
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
