using CertEmpire.DTOs.SimulationDTOs;
using CertEmpire.Helpers.ResponseWrapper;

namespace CertEmpire.Interfaces
{
    public interface ISimulationRepo
    {
        Task<Response<ExamDTO>> PracticeOnline(Guid fileId);
    }
}