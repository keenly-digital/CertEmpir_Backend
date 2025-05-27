using CertEmpire.DTOs.UserDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Models;

namespace CertEmpire.Interfaces
{
    public interface IUserRepo : IRepository<User>
    {
        Task<Response<string>> AddUser(AddUserRequest request);
        Task<Response<LoginResponse>> LoginResponse(LoginRequest request);
        Task<Response<object>> GetAllEmailAsync();
        Task<Response<string>> DeleteUser(string Email);
        Task<Response<string>> UpdatePassword(UpdatePasswordRequest request);
        Task<Response<AdminLoginResponse>> AdminLoginResponse(AdminLoginRequest request);
    }
}