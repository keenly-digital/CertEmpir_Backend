using CertEmpire.Data;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Models;
using Microsoft.EntityFrameworkCore;

namespace CertEmpire.Services
{
    public class UploadedFileRepo(ApplicationDbContext context) : Repository<UploadedFile>(context), IUploadedFileRepo
    {
        public async Task<Response<UploadedFile>> GetFileByFileUrl(string fileUrl)
        {
            Response<UploadedFile> response = new();
            var resullt = await _context.UploadedFiles.FirstOrDefaultAsync(x => x.FileURL == fileUrl);
            if (resullt != null)
            {
                response = new Response<UploadedFile>(true, "File found.", "", resullt);
            }
            else
            {
                response = new Response<UploadedFile>(false, "File not found.", "", default);
            }
            return response;
        }

        public async Task<Response<UploadedFile>> GetFileById(Guid fileId)
        {
            var result = await _context.UploadedFiles.FirstOrDefaultAsync(x => x.FileId == fileId);
            if (result != null)
            {
                return new Response<UploadedFile>(true, "File found.", "", result);
            }
            else
            {
                return new Response<UploadedFile>(false, "File not found.", "", default);
            }
        }
    }
}