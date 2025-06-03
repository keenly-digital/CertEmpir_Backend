using DocumentFormat.OpenXml.Spreadsheet;

namespace CertEmpire.Services.EmailService
{
    public interface IEmailService
    {
        void SendEmail(Email email);
    }
}