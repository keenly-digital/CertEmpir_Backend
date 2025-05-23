using CertEmpire.Data;
using CertEmpire.DTOs.UserDTOs;
using CertEmpire.Helpers.Enums;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Interfaces.IJwtService;
using CertEmpire.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace CertEmpire.Services
{
    public class UserRepo : Repository<User>, IUserRepo
    {
        private readonly IUploadedFileRepo _uploadFileRepo;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;
        public UserRepo(ApplicationDbContext context, IUploadedFileRepo _uploadFileRepo, IJwtService jwtService, IConfiguration configuration) : base(context)
        {
            this._uploadFileRepo = _uploadFileRepo;
            this._jwtService = jwtService;
            _configuration = configuration;
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

        public async Task<Response<object>> GetAllEmailAsync()
        {
            Response<object> response = new();
            Dictionary<string, string> userObject = [];
            var emails = await _context.Users.ToListAsync();
            if (emails.Count > 0)
            {
                foreach (var item in emails)
                {
                    userObject.Add(item.Email, item.Password);
                }

                response = new Response<object>(true, "Emails retrieved successfully", "", userObject);
            }
            else
            {
                response = new Response<object>(false, "No emails found", "", default);
            }
            return response;
        }

        public async Task<Response<LoginResponse>> LoginResponse(LoginRequest request)
        {
            var response = new Response<LoginResponse>();
            string fileUrl = string.Empty;
            bool simulation = false;
            int productId = 0;
            if (request == null)
            {
                return new Response<LoginResponse>(false, "Request can't be null", "", default);
            }
            var user = _context.Users.FirstOrDefault(u => u.Email == request.Email && u.Password == request.Password);
            if (user == null)
            {
                return new Response<LoginResponse>(false, "Invalid email or password", "", default);
            }
            if (request.File != null && request.File.Count != 0)
            {
                foreach (var file in request.File)
                {
                    var fileExist = await _context.UploadedFiles
                        .FirstOrDefaultAsync(x => x.ProductId == file.ProductId);
                    Guid fileId;
                    if (fileExist == null)
                    {
                        var uploadedFile = new UploadedFile
                        {
                            FileURL = file.FileUrl??"",
                            FilePrice = file.FilePrice,
                            FileId = Guid.NewGuid(),
                            FileName = file.FileUrl.Split('/').Last(),
                            ProductId = file.ProductId,
                            Simulation = true
                        };
                        await _context.UploadedFiles.AddAsync(uploadedFile);
                        await _context.SaveChangesAsync();
                        fileId = uploadedFile.FileId;
                        simulation = uploadedFile.Simulation;
                        productId = uploadedFile.ProductId;
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
                }
            }
            var jwtToken = await _jwtService.generateJwtToken(user);
            return new Response<LoginResponse>(
                true,
                "User logged in successfully",
                "",
                new LoginResponse { UserId = user.UserId, Simulation = simulation, JwtToken = jwtToken, ProductId = productId });
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