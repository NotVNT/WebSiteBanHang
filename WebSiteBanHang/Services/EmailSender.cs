using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace WebSiteBanHang.Services
{
    public class EmailSender : ICustomEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings").Get<EmailSettings>();

                using (var client = new SmtpClient())
                {
                    client.Host = "smtp.gmail.com";
                    client.Port = 587;
                    client.EnableSsl = true;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(
                        "tainguvcl113@gmail.com",
                        "xdbv dpsh ooph imlp"
                    );

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress("tainguvcl113@gmail.com", "WebSiteBanHang");
                        mailMessage.To.Add(email);
                        mailMessage.Subject = subject;
                        mailMessage.Body = message;
                        mailMessage.IsBodyHtml = true;

                        await client.SendMailAsync(mailMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to send email: {ex.Message}", ex);
            }
        }
    }

    public class EmailSettings
    {
        public string Mail { get; set; }
        public string DisplayName { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
    }
} 