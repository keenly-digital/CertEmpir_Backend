using CertEmpire.Data;
using CertEmpire.DTOs.UserRoleDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Models;
using Microsoft.EntityFrameworkCore;

namespace CertEmpire.Services
{
    public class UserRoleRepo(ApplicationDbContext context) : Repository<UserRole>(context), IUserRoleRepo
    {
        public async Task<Response<object>> GetAllRoles(int pageNumber)
        {
            Response<object> response = new();
            int pageSize = pageNumber * 10;
            var totalUser = await _context.UserRoles.CountAsync();
            var userRoles = await _context.UserRoles.Take(pageSize).ToListAsync();
            if (userRoles.Any())
            {
                List<AddUserRoleResponse> roles = userRoles.Select(role => new AddUserRoleResponse
                {
                    UserRoleId = role.UserRoleId,
                    UserRoleName = role.UserRoleName,
                    UserManagement = role.UserManagement,
                    Tasks = role.Tasks,
                    FileCreation = role.FileCreation,
                    Create = role.Create,
                    Edit = role.Edit,
                    Delete = role.Delete
                }).ToList();
                var res = new
                {
                    result = totalUser,
                    data = roles
                };
                response = new Response<object>(true, "Roles retrieved successfully.", "", res);
            }
            else
            {
                response = new Response<object>(false, "No roles found.", "There are no user roles available.", null);
            }
            return response;
        }
        public async Task<Response<AddUserRoleResponse>> AddRole(AddUserRoleRequest request)
        {
            Response<AddUserRoleResponse> response = new();
            var userRole = await _context.UserRoles.FirstOrDefaultAsync(x => x.UserRoleName == request.UserRoleName);
            if (userRole != null)
            {
                AddUserRoleResponse res = new()
                {
                    UserRoleId = userRole.UserRoleId,
                    UserRoleName = userRole.UserRoleName,
                    UserManagement = userRole.UserManagement,
                    Tasks = userRole.Tasks,
                    FileCreation = userRole.FileCreation,
                    Create = userRole.Create,
                    Edit = userRole.Edit,
                    Delete = userRole.Delete
                };
                response = new Response<AddUserRoleResponse>(true, "Role already exist.","", res);
            }
            else
            {
                UserRole newUserRole = new()
                {
                    UserRoleName = request.UserRoleName,
                    FileCreation = request.FileCreation,
                    Tasks = request.Tasks,
                    UserManagement = request.UserManagement,
                    Edit = request.Edit,
                    Delete = request.Delete,
                    Create = request.Create,
                    UserRoleId = Guid.NewGuid()
                };
                var result = await AddAsync(newUserRole);
                if (result != null)
                {
                    AddUserRoleResponse res = new()
                    {
                        UserRoleId = result.UserRoleId,
                        UserRoleName = result.UserRoleName,
                        UserManagement = result.UserManagement,
                        Tasks = result.Tasks,
                        FileCreation = result.FileCreation,
                        Create = result.Create,
                        Edit = result.Edit,
                        Delete = result.Delete
                    };
                    response = new Response<AddUserRoleResponse>(true, "New role created successfully.", "", res);
                }
                else
                {
                    response = new Response<AddUserRoleResponse>(false, "Failed to create new role.", "An error occurred while creating the role.", null);
                }               
            }
            return response;
        }
        public async Task<Response<string>> DeleteRole(Guid RoleId)
        {
            Response<string> response = new();
            var role = await _context.UserRoles.FirstOrDefaultAsync(x=>x.UserRoleId.Equals(RoleId));
            if (role != null)
            {
                await DeleteAsync(role);
                response = new Response<string>(true,"Role deleted.","","");
            }
            else
            {
                response = new Response<string>(true, "Role can't be deleted.", "An error occured while deleting the role.", "");
            }
            return response;
        }
    }
}