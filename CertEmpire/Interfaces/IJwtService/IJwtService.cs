using CertEmpire.Models;

namespace CertEmpire.Interfaces.IJwtService
{
    public interface IJwtService
    {
        Task<string> generateJwtToken(User user);
    }
}
