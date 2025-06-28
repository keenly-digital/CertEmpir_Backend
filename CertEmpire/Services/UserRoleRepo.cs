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
        public async Task<Response<object>> GetAllRoles(int pageNumber, bool isAll)
        {
            Response<object> response = new();
            int pageSize = pageNumber * 10;
            List<UserRole> userRoles = new List<UserRole>();
            //If isAll is true, fetch all roles; otherwise, fetch roles based on page size
            if (isAll == true)
            {
                userRoles = await _context.UserRoles.ToListAsync();
            }
            else
            {
                userRoles = await _context.UserRoles.Take(pageSize).ToListAsync();
            }
            var totalUser = await _context.UserRoles.CountAsync();
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
                response = new Response<AddUserRoleResponse>(true, "Role already exist.", "", res);
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
        public async Task<Response<EditUserRoleResponse>> EditRole(EditUserRoleResponse request)
        {
            Response<EditUserRoleResponse> response = new();
            var userRole = await _context.UserRoles.FirstOrDefaultAsync(x => x.UserRoleName == request.UserRoleName);
            if (userRole != null)
            {

                userRole.UserRoleName = string.IsNullOrWhiteSpace(userRole.UserRoleName)
                    ? request.UserRoleName
                    : userRole.UserRoleName;
                userRole.UserManagement = request.UserManagement ?? userRole.UserManagement;
                userRole.Tasks = request.Tasks ?? userRole.Tasks;
                userRole.FileCreation = request.FileCreation ?? userRole.FileCreation;
                userRole.Create = request.Create ?? userRole.Create;
                userRole.Edit = request.Edit ?? userRole.Edit;
                userRole.Delete = request.Delete ?? userRole.Delete;
                _context.UserRoles.Update(userRole);
                await _context.SaveChangesAsync();
                //EditUserRoleResponse res1 = new()
                //{
                //    UserRoleId = userRole.UserRoleId,
                //    UserRoleName = userRole.UserRoleName,
                //    UserManagement = userRole.UserManagement,
                //    Tasks = userRole.Tasks,
                //    FileCreation = userRole.FileCreation,
                //    Create = userRole.Create,
                //    Edit = userRole.Edit,
                //    Delete = userRole.Delete
                //};
                response = new Response<EditUserRoleResponse>(true, "Role Updated.", "", default);
            }
            else
            {
                response = new Response<EditUserRoleResponse>(true, " No Role found.", "", default);
            }

            return response;
        }
        public async Task<Response<string>> DeleteRole(Guid RoleId)
        {
            Response<string> response = new();
            var role = await _context.UserRoles.FirstOrDefaultAsync(x => x.UserRoleId.Equals(RoleId));
            if (role != null)
            {
                var linkedAccounts = await _context.Users.Where(x => x.UserRoleId.Equals(RoleId)).ToListAsync();
                if (linkedAccounts.Any())
                {
                    response = new Response<string>(true, "Role can't be deleted because this role linked with other accounts.", "", "");
                }
                else
                {
                    await DeleteAsync(role);
                    response = new Response<string>(true, "Role deleted.", "", "");
                }
            }
            else
            {
                response = new Response<string>(true, "Role can't be deleted.", "An error occured while deleting the role.", "");
            }
            return response;
        }
    }
}