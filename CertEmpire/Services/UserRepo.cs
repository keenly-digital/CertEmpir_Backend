using CertEmpire.Data;
using CertEmpire.DTOs.UserDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Interfaces.IJwtService;
using CertEmpire.Models;
using Microsoft.EntityFrameworkCore;

namespace CertEmpire.Services
{
    public class UserRepo : Repository<User>, IUserRepo
    {
        private readonly IUploadedFileRepo _uploadFileRepo;
        private readonly IJwtService _jwtService;
        public UserRepo(ApplicationDbContext context,IUploadedFileRepo _uploadFileRepo, IJwtService jwtService) : base(context)
        {
            this._uploadFileRepo = _uploadFileRepo;
            this._jwtService = jwtService;
        }
        public async Task<Response<string>> AddUser(AddUserRequest request)
        {
            Response<string> response = new();
            if (request == null)
            {
                response = new Response<string>(false, "Request can't be null", "", default);
            }
            else
            {
                var checkUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (checkUser != null)
                {
                    response = new Response<string>(true, "User exist", "", default);
                }
                else
                {
                    User user = new()
                    {
                        Email = request.Email,
                        Password = request.Password,
                        UserId = Guid.NewGuid()
                    };
                    var result = await AddAsync(user);
                    if (result != null)
                    {
                        response = new Response<string>(true, "User added successfully", "", default);
                    }
                    else
                    {
                        response = new Response<string>(true, "Error while adding user.", "", default);
                    }
                }   
            }
            return response;
        }

        public async Task<Response<GetAllEmailResponse>> GetAllEmailAsync()
        {
            Response<GetAllEmailResponse> response = new();
            List<string> EmailList = [];
            var emails = await _context.Users.Select(u => u.Email).ToListAsync();
            if (emails.Count > 0)
            {
                EmailList.AddRange(emails);
                response = new Response<GetAllEmailResponse>(true, "Emails retrieved successfully", "", new GetAllEmailResponse { Emails = EmailList });
            }
            else
            {
                response = new Response<GetAllEmailResponse>(false, "No emails found", "", default);
            }
            return response;
        }

        public async Task<Response<AddUserResponse>> LoginResponse(LoginRequest request)
        {
            var response = new Response<AddUserResponse>();
            var FileObj = new List<FileResponseObject>();

            if (request == null)
            {
                return new Response<AddUserResponse>(false, "Request can't be null", "", default);
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == request.Email && u.Password == request.Password);
            if (user == null)
            {
                return new Response<AddUserResponse>(false, "Invalid email or password", "", default);
            }

            if (request.File != null && request.File.Count != 0)
            {
                foreach (var file in request.File)
                {
                    if (file.FileUrl == null) continue;

                    var fileExist = await _context.UploadedFiles
                        .FirstOrDefaultAsync(x => x.FileURL == file.FileUrl);

                    Guid fileId;

                    if (fileExist == null)
                    {
                        var uploadedFile = new UploadedFile
                        {
                            FileURL = file.FileUrl,
                            FilePrice = file.FilePrice,
                            FileId = Guid.NewGuid(),
                            FileName = file.FileUrl.Split('/').Last()
                        };

                        await _context.UploadedFiles.AddAsync(uploadedFile);
                        await _context.SaveChangesAsync();

                        fileId = uploadedFile.FileId;
                    }
                    else
                    {
                        fileId = fileExist.FileId;
                    }

                    // Check if user already linked to this file
                    var alreadyLinked = await _context.UserFilePrices
                        .AnyAsync(u => u.UserId == user.UserId && u.FileId == fileId);

                    if (!alreadyLinked)
                    {
                        UserFilePrice filePrice = new()
                        {
                            FileId = fileId,
                            UserId = user.UserId,
                            FilePriceId = Guid.NewGuid()
                        };
                        await _context.UserFilePrices.AddAsync(filePrice);
                        await _context.SaveChangesAsync();
                    }

                    // Get all buyers (excluding current user optionally)
                    var buyers = await _context.UserFilePrices
                        .Where(u => u.FileId == fileId && u.UserId != user.UserId)
                        .Select(u => u.UserId)
                        .ToListAsync();

                    FileObj.Add(new FileResponseObject
                    {
                        FileId = fileId,
                        FileUrl = file.FileUrl,
                    });
                }
            }
            var jwtToken = await _jwtService.generateJwtToken(user);
            return new Response<AddUserResponse>(
                true,
                "User logged in successfully",
                "",
                new AddUserResponse { FileObj = FileObj , JwtToken = jwtToken});
        }

        public async Task<Response<string>> UpdatePassword(UpdatePasswordRequest request)
        {
            Response<string> response = new();
            if (request == null)
            {
                response = new Response<string>(false, "Request can't be null", "", default);
            }
            else
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user != null)
                {
                    if (user.Password.Equals(request.OldPassword))
                    {
                        user.Password = request.NewPassword;
                        await UpdateAsync(user);
                        response = new Response<string>(true, "Password updated successfully", "", default);
                    }
                    else
                    {
                        response = new Response<string>(false, "Old password is incorrect", "", default);
                    }
                }
                else
                {
                    response = new Response<string>(false, "User not found", "", default);
                }
            }
            return response;
        }
        public async Task<Response<string>> DeleteUser(string Email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);
            if (user != null)
            {
                await DeleteAsync(user);
                return new Response<string>(true, "User deleted successfully", "", default);
            }
            else
            {
                return new Response<string>(false, "User not found", "", default);
            }
        } 
    }
}