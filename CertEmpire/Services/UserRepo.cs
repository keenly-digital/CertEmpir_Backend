using CertEmpire.Data;
using CertEmpire.DTOs.UserDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Interfaces.IJwtService;
using CertEmpire.Models;
using CertEmpire.Services.FileService;
using Microsoft.EntityFrameworkCore;

namespace CertEmpire.Services
{
    public class UserRepo : Repository<User>, IUserRepo
    {
        private readonly IUploadedFileRepo _uploadFileRepo;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;
        private readonly IFileService _fileService;
        public UserRepo(ApplicationDbContext context, IUploadedFileRepo _uploadFileRepo, IJwtService jwtService, IConfiguration configuration,
            IFileService fileService) : base(context)
        {
            this._uploadFileRepo = _uploadFileRepo;
            this._jwtService = jwtService;
            _configuration = configuration;
            _fileService = fileService;
        }

        #region User Auth Module
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
                        FirstName = "User",
                        LastName = "",
                        UserRole = "User",
                        ImageUrl = "",
                        IsAdmin = false,
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
                            FileURL = file.FileUrl ?? "",
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
        #endregion

        #region Admin Auth Module
        /// Admin Login
        public async Task<Response<AdminLoginResponse>> AdminLoginResponse(AdminLoginRequest request)
        {
            Response<AdminLoginResponse> response = new();
            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.Password == request.Password && u.IsAdmin.Equals(true));
            if (admin == null)
            {
                return new Response<AdminLoginResponse>(false, "Invalid email or password", "", default);
            }
            else
            {
                var jwtToken = await _jwtService.generateJwtToken(admin);
                return new Response<AdminLoginResponse>(
                    true,
                    "Admin logged in successfully",
                    "",
                    new AdminLoginResponse
                    {
                        UserId = admin.UserId,
                        Email = admin.Email,
                        FirstName = admin.FirstName,
                        LastName = admin.LastName,
                        UserRole = admin.UserRole,
                        JWToken = jwtToken
                    });
            }
        }
        //Change Email for Admin, Super Admin, Owner or for other roles except User
        public async Task<Response<string>> ChangeEmailAsync(ChangeEmailAsync request)
        {
            Response<string> response = new();
            if (request == null)
            {
                response = new Response<string>(false, "Request can't be null", "", default);
            }
            else
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.OldEmail);
                if (user != null)
                {
                    user.Email = request.NewEmail;
                    await UpdateAsync(user);
                    response = new Response<string>(true, "Email updated successfully", "", default);
                }
                else
                {
                    response = new Response<string>(false, "User not found", "", default);
                }
            }
            return response;
        }
        //Change First Name/Last Name for Admin, Super Admin, Owner or for other roles except User
        public async Task<Response<string>> ChangeNameAsync(ChangeFirstOrLastName request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId);
            if (user == null)
            {
                return new Response<string>(false, "User not found.", "", "");
            }
            bool isUpdated = false;
            if (!string.IsNullOrWhiteSpace(request.FirstName))
            {
                user.FirstName = request.FirstName.Trim();
                isUpdated = true;
            }
            if (!string.IsNullOrWhiteSpace(request.LastName))
            {
                user.LastName = request.LastName.Trim();
                isUpdated = true;
            }
            if (!isUpdated)
            {
                return new Response<string>(false, "No name provided to update.", "", "");
            }
            await UpdateAsync(user);
            return new Response<string>(true, "Name updated successfully", "", "");
        }
        //Change password for Admin, Super Admin, Owner or for other roles except User
        public async Task<Response<string>> ChangePasswordAsync(ChangePasswordAsync request)
        {
            Response<string> response = new();
            if (request == null)
            {
                response = new Response<string>(false, "Request can't be null", "", default);
            }
            else
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId);
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
        // Change Profile Picture for Admin, Super Admin, Owner or for other roles except User
        public async Task<Response<string>> ChangeProfilePicAsync(ChangeProfilePic request)
        {
            Response<string> response = new();
            if (request == null)
            {
                response = new Response<string>(false, "Request can't be null", "", default);
            }
            else
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId);
                if (user != null)
                {
                    if (request.Image != null)
                    {
                        var fileUrl = await _fileService.ChangeProfilePic(request);
                        user.ImageUrl = fileUrl;
                        await UpdateAsync(user);
                        response = new Response<string>(true, "Profile picture updated successfully", "", default);
                    }
                    else
                    {
                        response = new Response<string>(false, "Image can't be null", "", default);
                    }
                }
                else
                {
                    response = new Response<string>(false, "User not found", "", default);
                }
            }
            return response;
        }
        #endregion
    }
}