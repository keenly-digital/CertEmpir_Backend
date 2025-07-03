using CertEmpire.Data;
using CertEmpire.DTOs.UserDTOs;
using CertEmpire.DTOs.UserRoleDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Interfaces.IJwtService;
using CertEmpire.Models;
using CertEmpire.Services.FileService;
using Microsoft.EntityFrameworkCore;
using Supabase.Gotrue;
using User = CertEmpire.Models.User;

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
                        UserRoleId = Guid.Empty,
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
            bool simulation = true;
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
                        .FirstOrDefaultAsync(x => x.FileURL == file.FileUrl);
                    if (fileExist == null)
                    {
                        return new Response<LoginResponse>(false, "File not found.", "", default);
                    }
                    else
                    {
                        var alreadyLinked = await _context.UserFilePrices
                       .AnyAsync(u => u.UserId == user.UserId && u.FileId == fileExist.FileId);
                        if (!alreadyLinked)
                        {
                            UserFilePrice filePrice = new()
                            {
                                FileId = fileExist.FileId,
                                UserId = user.UserId,
                                FilePriceId = Guid.NewGuid(),
                                FilePrice = file.FilePrice,
                                OrderId = file.OrderId,
                                ProductId = file.ProductId
                            };
                            await _context.UserFilePrices.AddAsync(filePrice);
                            await _context.SaveChangesAsync();

                        }
                    }
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
        public async Task<Response<string>> GetUser(string Email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);
            if (user != null)
            {
                return new Response<string>(true, "User Found successfully", "", user.Password);
            }
            else
            {
                return new Response<string>(false, "User not found", "", default);
            }
        }
        #endregion

        #region Admin Auth Module
        public async Task<Response<AddNewUserResponse>> AddNewUserAsync(AddNewUserRequest request)
        {
            Response<AddNewUserResponse> response = new();
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email.Equals(request.Email));
            if (user != null)
            {
                AddNewUserResponse res = new()
                {
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Password = user.Password,
                    UserId = user.UserId,
                    UserName = user.UserName,
                    UserRoleId = user.UserRoleId
                };
                response = new Response<AddNewUserResponse>(true, "User already exist.", "", res);
            }
            else
            {
                var userRole = await _context.UserRoles.FirstOrDefaultAsync(x => x.UserRoleId.Equals(request.UserRoleId));
                if (userRole == null)
                {
                    return new Response<AddNewUserResponse>(false, "User role does not exist.", "", default);
                }
                bool isAdmin;
                if (userRole.UserRoleName.Equals("Admin") || userRole.UserRoleName.Equals("SuperAdmin"))
                {
                    isAdmin = true;
                }
                else
                {
                    isAdmin = false;
                }
                User newUser = new()
                {
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    ImageUrl = "",
                    IsAdmin = isAdmin,
                    UserRoleId = userRole.UserRoleId,
                    Password = request.Password,
                    UserName = request.UserName,
                    UserId = Guid.NewGuid()
                };
                var result = await AddAsync(newUser);
                if (result != null)
                {
                    AddNewUserResponse res = new()
                    {
                        Email = result.Email,
                        FirstName = result.FirstName,
                        LastName = result.LastName,
                        Password = result.Password,
                        UserId = result.UserId,
                        UserName = result.UserName,
                        UserRoleId = result.UserRoleId
                    };
                    response = new Response<AddNewUserResponse>(true, "User added.", "", res);
                }
                else
                {
                    response = new Response<AddNewUserResponse>(false, "Error while adding user.", "", default);
                }
            }
            return response;
        }

        /// Admin Login
        public async Task<Response<AdminLoginResponse>> AdminLoginResponse(AdminLoginRequest request)
        {
            Response<AdminLoginResponse> response = new();
            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.Password == request.Password);
            if (admin == null)
            {
                return new Response<AdminLoginResponse>(false, "Invalid email or password", "", default);
            }
            else
            {
                var userRole = await _context.UserRoles.FirstOrDefaultAsync(x => x.UserRoleId.Equals(admin.UserRoleId));
                if (userRole == null)
                {
                    response = new Response<AdminLoginResponse>(false, "User role not found.", "", default);
                }
                else
                {
                    Permissions permissions = new()
                    {
                        Create = userRole.Create,
                        Delete = userRole.Delete,
                        Edit = userRole.Edit,
                        FileCreation = userRole.FileCreation,
                        Tasks = userRole.Tasks,
                        UserManagement = userRole.UserManagement
                    };
                    var jwtToken = await _jwtService.generateJwtToken(admin);
                    response = new Response<AdminLoginResponse>(
                        true,
                        "Admin logged in successfully",
                        "",
                        new AdminLoginResponse
                        {
                            UserId = admin.UserId,
                            Email = admin.Email,
                            FirstName = admin.FirstName,
                            LastName = admin.LastName,
                            UserRole = userRole.UserRoleName ?? "Admin",
                            JWToken = jwtToken,
                            Permissions = permissions
                        });
                }
                return response;
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
                        response = new Response<string>(true, "Profile picture updated successfully", "", fileUrl);
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
        public async Task<Response<string>> DeleteUser(Guid userId)
        {
            Response<string> response = new();
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId.Equals(userId));
            if (user == null)
            {
                response = new Response<string>(true, "User not found or already deleted.", "", "");
            }
            else
            {
                await DeleteAsync(user);
                response = new Response<string>(true, "User deleted.", "", "");
            }
            return response;
        }

        public async Task<Response<object>> GetAllUsersAsync(Guid UserId, int PageNumber)
        {
            Response<object> response = new();
            int pageSize = PageNumber * 10;
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId.Equals(UserId) && x.IsAdmin.Equals(true));
            if (user != null)
            {
                var totalUsers = await _context.Users.Where(x => x.UserId!=UserId).ToListAsync();
                var pageUser = totalUsers.Take(pageSize);
                int totalCount = totalUsers.Count();
                List<GetAllUsersResponse> userList = new();
                foreach (var userInfo in pageUser)
                {
                    var userRole = await _context.UserRoles.FirstOrDefaultAsync(x => x.UserRoleId.Equals(userInfo.UserRoleId));

                    GetAllUsersResponse userObj = new()
                    {
                        UserId = userInfo.UserId,
                        Name = $"{userInfo.FirstName} {userInfo.LastName}",
                        Email = userInfo.Email,
                        ProfilePicUrl = userInfo.ImageUrl,
                        CreatedAt = userInfo.Created,
                        Role = userRole.UserRoleName??"User"
                    };
                    userList.Add(userObj);
                    var res = new
                    {
                        result = totalCount,
                        data = userList
                    };
                    response = new Response<object>(true, "Users retrieved successfully", "", res);
                }
            }
            else
            {
                response = new Response<object>(false, "No user found", "", default);
            }
            return response;
        }

        public async Task<Response<object>> EditUser(EditUser request)
        {
            Response<object> response = new();
            var userInfo = await _context.Users.FirstOrDefaultAsync(x => x.UserId.Equals(request.UserId));
            if (userInfo == null)
            {
                response = new Response<object>(true, "No user found.", "", "");
            }
            else
            {
                var roleInfo = await _context.UserRoles.FirstOrDefaultAsync(x => x.UserRoleName.Equals(request.RoleName));
                if (roleInfo == null)
                {
                    response = new Response<object>(true, "No role found.", "", "");
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(request.FirstName))
                    {
                        userInfo.FirstName = request.FirstName.Trim();
                    }
                    if (!string.IsNullOrWhiteSpace(request.LastName))
                    {
                        userInfo.LastName = request.LastName.Trim();
                    }
                    if (!string.IsNullOrEmpty(request.Email))
                    {
                        userInfo.Email = request.Email.Trim();
                    }
                    userInfo.UserRoleId = roleInfo.UserRoleId;
                    _context.Users.Update(userInfo);
                    await _context.SaveChangesAsync();
                    response = new Response<object>(true, "Updated.", "", "");
                }
            }
            return response;
        }
        #endregion
    }
}