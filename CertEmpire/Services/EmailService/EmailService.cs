using System.Net.Mail;
using System.Net;

namespace CertEmpire.Services.EmailService
{
    public class EmailService : IEmailService
    {
        public void SendEmail(Email email)
        {
            SmtpClient client = new SmtpClient("smtp.gmail.com")
            {
                UseDefaultCredentials = true,
                Port = 587,
                Credentials = new NetworkCredential("hmz7418819@gmail.com", "hcur uwwd dbdo tyuy"),
                EnableSsl = true
            };
            client.Send("hmz7418819@gmail.com", email.To, email.Subject, email.Body);
        }
    }
}