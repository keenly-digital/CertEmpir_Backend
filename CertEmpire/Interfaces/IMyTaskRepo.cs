using CertEmpire.DTOs.MyTaskDTOs;
using CertEmpire.Helpers.Enums;
using CertEmpire.Helpers.ResponseWrapper;

namespace CertEmpire.Interfaces
{
    public interface IMyTaskRepo
    {
        Task<Response<object>> GetPendingTasks(TaskFilterDTO request);
        Task<Response<string>> SubmitVote(SubmitVoteDTO request);
    }
}
