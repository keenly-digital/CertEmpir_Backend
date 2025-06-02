using CertEmpire.Data;
using CertEmpire.DTOs.DomainDTOs;
using CertEmpire.Helpers.ResponseWrapper;
using CertEmpire.Interfaces;
using CertEmpire.Models;
using Microsoft.EntityFrameworkCore;

namespace CertEmpire.Services
{
    public class DomainRepo(ApplicationDbContext context) : Repository<Domain>(context), IDomainRepo
    {
        public async Task<Response<List<AddDomainResponse>>> GetAllDomain(int PageNumber, int PageSize)
        {
            Response<List<AddDomainResponse>> response = new();
            List<AddDomainResponse> list = new List<AddDomainResponse>();
            var domainList = _context.Domains.AsQueryable().Where(x=>x.IsActive.Equals(true)).Skip((PageNumber - 1) * PageSize).Take(PageSize);
            foreach (var item in domainList)
            {
                AddDomainResponse domainResponse = new()
                {
                    DomainId = item.DomainId,
                    DomainName = item.DomainName,
                    IncludeAnswers = item.IncludeAnswers,
                    IncludeComments = item.IncludeComments,
                    IncludeExplanations = item.IncludeExplanations,
                    IncludeQuestions = item.IncludeQuestions,
                    IsActive = item.IsActive,
                    DomainUrl = item.DomainURL
                };
                list.Add(domainResponse);
            }
            response = new Response<List<AddDomainResponse>>(true, "Domains List", "", list);
            return response;
        }
        public async Task<Response<AddDomainResponse>> GetDomainByName(string domainName)
        {
            Response<AddDomainResponse> response = new Response<AddDomainResponse>();
            var domain = await _context.Domains.FirstOrDefaultAsync(x => x.DomainName.Equals(domainName));
            if (domain != null)
            {
                AddDomainResponse domainResponse = new()
                {
                    DomainId = domain.DomainId,
                    DomainName = domain.DomainName,
                    IncludeAnswers = domain.IncludeAnswers,
                    IncludeComments = domain.IncludeComments,
                    IncludeExplanations = domain.IncludeExplanations,
                    IncludeQuestions = domain.IncludeQuestions,
                    IsActive = domain.IsActive,
                    DomainUrl = domain.DomainURL
                };
                response = new Response<AddDomainResponse>(true, "Domain found.", "", domainResponse);
            }
            else
            {
                response = new Response<AddDomainResponse>(false, "Domain not found.", "", default);
            }
            return response;
        }
        public async Task<Response<AddDomainResponse>> GetDomainById(Guid domainId)
        {
            Response<AddDomainResponse> response = new Response<AddDomainResponse>();
            var domain = await _context.Domains.FirstOrDefaultAsync(x => x.DomainId.Equals(domainId));
            if (domain != null)
            {
                AddDomainResponse domainResponse = new()
                {
                    DomainId = domain.DomainId,
                    DomainName = domain.DomainName,
                    IncludeAnswers = domain.IncludeAnswers,
                    IncludeComments = domain.IncludeComments,
                    IncludeExplanations = domain.IncludeExplanations,
                    IncludeQuestions = domain.IncludeQuestions,
                    IsActive = domain.IsActive,
                    DomainUrl = domain.DomainURL
                };
                response = new Response<AddDomainResponse>(true, "Domain found.", "", domainResponse);
            }
            else
            {
                response = new Response<AddDomainResponse>(false, "Domain not found.", "", default);
            }
            return response;
        }
        public async Task<Response<AddDomainResponse>> AddDomain(AddDomainRequest request)
        {
            Response<AddDomainResponse> response = new Response<AddDomainResponse>();
            if (request == null || string.IsNullOrEmpty(request.DomainName))
            {
                response = new Response<AddDomainResponse>(false, "Invalid request data.\"Domain name cannot be null or empty.", "", default);
            }
            else
            {
                var domain = await _context.Domains.FirstOrDefaultAsync(x => x.DomainName.Equals(request.DomainName));
                if (domain != null)
                {
                    AddDomainResponse domainResponse = new()
                    {
                        DomainId = domain.DomainId,
                        DomainName = domain.DomainName,
                        IncludeAnswers = domain.IncludeAnswers,
                        IncludeComments = domain.IncludeComments,
                        IncludeExplanations = domain.IncludeExplanations,
                        IncludeQuestions = domain.IncludeQuestions,
                        IsActive = domain.IsActive,
                        DomainUrl = domain.DomainURL
                    };
                    response = new Response<AddDomainResponse>(true, "Domain is already exist.", "", domainResponse);
                }
                else
                {
                    Domain data = new()
                    {
                        DomainId = Guid.NewGuid(),
                        DomainName = request.DomainName,
                        IncludeAnswers = request.IncludeAnswers,
                        IncludeComments = request.IncludeComments,
                        IncludeExplanations = request.IncludeExplanations,
                        IncludeQuestions = request.IncludeQuestions,
                        IsActive = request.IsActive,
                        DomainURL = request.DomainUrl
                    };
                    var result = await AddAsync(data);
                    if (result != null)
                    {
                        AddDomainResponse domainResponse = new()
                        {
                            DomainId = result.DomainId,
                            DomainName = result.DomainName,
                            IncludeAnswers = result.IncludeAnswers,
                            IncludeComments = result.IncludeComments,
                            IncludeExplanations = result.IncludeExplanations,
                            IncludeQuestions = result.IncludeQuestions,
                            IsActive = result.IsActive,
                            DomainUrl = result.DomainURL
                        };
                        response = new Response<AddDomainResponse>(true, "Domain is already exist.", "", domainResponse);
                    }
                    else
                    {
                        response = new Response<AddDomainResponse>(false, "Failed to add domain.", "", default);
                    }
                }
            }
            return response;
        }
        public async Task<Response<string>> EditDomain(EditDomainRequest request)
        {
            if (request == null || request.DomainId == Guid.Empty)
                return new Response<string>(false, "Invalid request. Domain ID is required.", "", "");

            var domain = await _context.Domains.FirstOrDefaultAsync(d => d.DomainId == request.DomainId);
            if (domain == null)
                return new Response<string>(false, "Domain not found.", "", "");

            if (!string.IsNullOrWhiteSpace(request.DomainName) && request.DomainName != domain.DomainName)
                domain.DomainName = request.DomainName;   
            
            if (!string.IsNullOrWhiteSpace(request.DomainUrl) && request.DomainUrl != domain.DomainURL)
                domain.DomainURL = request.DomainUrl;

            if (request.IncludeQuestions.HasValue)
                domain.IncludeQuestions = request.IncludeQuestions.Value;

            if (request.IncludeAnswers.HasValue)
                domain.IncludeAnswers = request.IncludeAnswers.Value;

            if (request.IncludeExplanations.HasValue)
                domain.IncludeExplanations = request.IncludeExplanations.Value;

            if (request.IncludeComments.HasValue)
                domain.IncludeComments = request.IncludeComments.Value;
            
            if(request.IsActive.HasValue)
                domain.IsActive = request.IsActive.Value;

            await UpdateAsync(domain);

            return new Response<string>(true, "Domain updated successfully.", "", "");
        }
        public async Task<Response<string>> DeletDomain(Guid domainId)
        {
            Response<string> response = new Response<string>();
            var domain = await _context.Domains.FirstOrDefaultAsync(x => x.DomainId.Equals(domainId));
            if (domain != null)
            {
                await DeleteAsync(domain);
                response = new Response<string>(true, "Domain deleted.", "", "");
            }
            else
            {
                response = new Response<string>(false, "Domain not found or already deleted.", "", default);
            }
            return response;
        }
    }
}