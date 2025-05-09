using CertEmpire.Data;
using CertEmpire.DTOs.UserDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Models;
using Microsoft.EntityFrameworkCore;

namespace CertEmpire.Services
{
    public class UserRepo(ApplicationDbContext context) : Repository<User>(context), IUserRepo
    {
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
            List<string> EmailList = new();
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
            Response<AddUserResponse> response;
            List<FileResponseObject> FileObj = new List<FileResponseObject>();
            if (request == null)
            {
                response = new Response<AddUserResponse>(false, "Request can't be null", "", default);
            }
            else
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == request.Email && u.Password == request.Password);
                if (user != null)
                {
                    if (request.File != null)
                        if (request.File.Any())
                        {
                            foreach (var file in request.File)
                            {
                                var uploadedFile = new UploadedFile
                                {
                                    FilePath = file.FileUrl ?? "",
                                    FilePrice = file.FilePrice,
                                    UserId = user.UserId,
                                    FileId = Guid.NewGuid(),
                                    FileName = file.FileUrl?.Split('/').Last() ?? ""
                                };
                                await _context.UploadedFiles.AddAsync(uploadedFile);
                                FileObj.Add(new FileResponseObject
                                {
                                    FileId = uploadedFile.FileId,
                                    FileUrl = uploadedFile.FilePath
                                });
                            }
                            await _context.SaveChangesAsync();
                        }
                    response = new Response<AddUserResponse>(true, "User logged in successfully", "", new AddUserResponse { FileObj = FileObj });
                }
                else
                {
                    response = new Response<AddUserResponse>(false, "Invalid email or password", "", default);
                }               
            }
            return response;
        }
    }
}