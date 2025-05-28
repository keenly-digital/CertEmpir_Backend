using CertEmpire.DTOs.UserDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Models;

namespace CertEmpire.Interfaces
{
    public interface IUserRepo : IRepository<User>
    {
        #region User Auth Interfaces
        Task<Response<string>> AddUser(AddUserRequest request);
        Task<Response<LoginResponse>> LoginResponse(LoginRequest request);
        Task<Response<object>> GetAllEmailAsync();
        Task<Response<string>> DeleteUser(string Email);
        Task<Response<string>> UpdatePassword(UpdatePasswordRequest request);
        #endregion

        #region Admin Auth Interfaces
        Task<Response<AdminLoginResponse>> AdminLoginResponse(AdminLoginRequest request);
        Task<Response<string>> ChangeEmailAsync(ChangeEmailAsync request);
        Task<Response<string>> ChangeNameAsync(ChangeFirstOrLastName request);
        Task<Response<string>> ChangePasswordAsync(ChangePasswordAsync request);
        Task<Response<string>> ChangeProfilePicAsync(ChangeProfilePic request);
        #endregion
    }
}