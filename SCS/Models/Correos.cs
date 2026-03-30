using Microsoft.AspNetCore.Identity.UI.Services;
using FluentEmail.Core;
using FluentEmail.Smtp;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using SCS.Models;

namespace SCS.Models
{
    public class Correos : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public Correos(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private bool EsDominioOutlookEmpresarial(string dominio)
        {
            var dominiosOutlookEmpresariales = new List<string>
            {
                "intel.com",
                "otroempresa.com",
                "microsoft.com",
                "office365.com",
                "outlook.com"
            };

            return dominiosOutlookEmpresariales.Any(d => dominio.EndsWith(d));
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var smtpSettings = GetSmtpSettings(email);

            if (smtpSettings == null)
            {
                throw new InvalidOperationException("No se pudo obtener las configuraciones de SMTP para el dominio del correo proporcionado.");
            }

            var sender = new SmtpSender(() => new SmtpClient(smtpSettings.Host)
            {
                Port = smtpSettings.Port,
                Credentials = new NetworkCredential(smtpSettings.Username, smtpSettings.Password),
                EnableSsl = smtpSettings.EnableSsl
            });

            FluentEmail.Core.Email.DefaultSender = sender;

            var emailResponse = await FluentEmail.Core.Email
                .From(smtpSettings.Username)
                .To(email)
                .Subject(subject)
                .Body(htmlMessage, true)
                .SendAsync();

            if (!emailResponse.Successful)
            {
                throw new InvalidOperationException($"Error al enviar el correo: {string.Join(", ", emailResponse.ErrorMessages)}");
            }
        }

        private SmtpSettings GetSmtpSettings(string email)
        {
            var emailDomain = email.Split('@')[1].ToLower();

            if (emailDomain.Contains("gmail.com"))
            {
                return _configuration.GetSection("SmtpSettings:Gmail").Get<SmtpSettings>();
            }
            else if (emailDomain.Contains("outlook.com") || emailDomain.Contains("hotmail.com") || EsDominioOutlookEmpresarial(emailDomain))
            {
                return _configuration.GetSection("SmtpSettings:Outlook").Get<SmtpSettings>();
            }

            throw new InvalidOperationException($"No se encontró configuración SMTP para el dominio: {emailDomain}");
        }
    }
}