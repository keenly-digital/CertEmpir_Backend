using CertEmpire.DTOs.SimulationDTOs;
using CertEmpire.Helpers.ResponseWrapper;

namespace CertEmpire.Interfaces
{
    public interface ISimulationRepo
    {
        Task<Response<object>> PracticeOnline(Guid fileId);
        Task<Response<object>> GetAllFiles(string email);
    }
}