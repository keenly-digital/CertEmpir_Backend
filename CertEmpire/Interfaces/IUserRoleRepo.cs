using CertEmpire.DTOs.UserRoleDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Models;

namespace CertEmpire.Interfaces
{
    public interface IUserRoleRepo : IRepository<UserRole>
    {
        Task<Response<List<AddUserRoleResponse>>> GetAllRoles();
        Task<Response<AddUserRoleResponse>> AddRole(AddUserRoleRequest request);
        Task<Response<string>> DeleteRole(Guid RoleId);
    }
}