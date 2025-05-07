using CertEmpire.DTOs.UserDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Models;

namespace CertEmpire.Interfaces
{
    public interface IUserRepo : IRepository<User>
    {
        Task<Response<string>> AddUser(AddUserRequest request);
        Task<Response<AddUserResponse>> LoginResponse(LoginRequest request);
        Task<Response<GetAllEmailResponse>> GetAllEmailAsync();
    }
}