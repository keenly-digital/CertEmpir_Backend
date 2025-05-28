using CertEmpire.DTOs.DomainDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Models;
using CertEmpire.Services;

namespace CertEmpire.Interfaces
{
    public interface IDomainRepo : IRepository<Domain>
    {
        Task<Response<List<AddDomainResponse>>> GetAllDomain(int PageNumber, int PageSize);
        Task<Response<AddDomainResponse>> GetDomainByName(string domainName);
        Task<Response<AddDomainResponse>> GetDomainById(Guid domainId);
        Task<Response<AddDomainResponse>> AddDomain(AddDomainRequest request);
        Task<Response<string>> EditDomain(EditDomainRequest request);
        Task<Response<string>> DeletDomain(Guid domainId);
    }
}