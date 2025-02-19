using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace GeoSit.Web.Api.EMail
{
    public class MailSender
    {
        private readonly SmtpClient _smtpClient;
        private readonly MailMessage _message;
        private MailSender(string subject, string body, bool isHTML) 
        {
            _smtpClient = new SmtpClient(ConfigurationManager.AppSettings["mail.smtp"], int.Parse(ConfigurationManager.AppSettings["mail.port"]))
            {
                Credentials = new NetworkCredential(ConfigurationManager.AppSettings["mail.user"], ConfigurationManager.AppSettings["mail.password"]),
                EnableSsl = bool.Parse(ConfigurationManager.AppSettings["mail.ssl"])
            };
            _message = new MailMessage()
            {
                From = new MailAddress(ConfigurationManager.AppSettings["mail.sender"]),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHTML
            };
        }
        internal static MailSender Create(string subject, string body, bool isHTML = false)
        {
            return new MailSender(subject, body, isHTML);
        }
        internal MailSender AddReceiver(string emailAddress) 
        { 
            ValidateAddress(emailAddress);
            _message.To.Add(new MailAddress(emailAddress));
            return this;
        }
        internal void Send()
        {
            using (_smtpClient)
            using (_message)
            {
                try
                {
                    if(!_message.To.Any()) throw new ArgumentException("No hay destinatarios a los que enviarles el email.");
                    _smtpClient.Send(_message);
                }
                catch (ArgumentException ex)
                {
                    Global.GetLogger().LogError("EnviarCorreo", ex);
                }
            }
        }

        private void ValidateAddress(string address)
        {
            if(string.IsNullOrEmpty(address?.Trim() ?? string.Empty))
            {
                throw new ArgumentException("No tiene la dirección de email configurada");
            }
            if(address.Length < 7) // cantidad basada en "a@a.com"
            {
                throw new ArgumentException($"La dirección de email configurada no es válida: {address}");
            }
            if(address.Contains(";"))
            {
                throw new ArgumentException($"La dirección de email configurada no especifica el dominio: {address}");
            }
            if (!address.Substring(1).Contains("@"))
            {
                throw new ArgumentException($"La dirección de email configurada no especifica el dominio: {address}");
            }
            if (Regex.IsMatch(address, @"/\.{2,}/"))
            {
                throw new ArgumentException($"La dirección de email configurada tiene '..' (2 puntos consecutivos): {address}");
            }

            string[] parts = address.Split('@');
            if (!Regex.IsMatch(parts[0], @"^[a-z0-9!#$%&\'*+\/=?^_`{|}~\.-]+$", RegexOptions.IgnoreCase))
            {
                throw new ArgumentException($"La dirección de email configurada tiene caracteres inválidos: {parts[0]}");
            }
            if(Regex.IsMatch(parts[1], @" \t\n\r\0\x0B."))
            {
                throw new ArgumentException($"La dirección de email configurada tiene un dominio que termina con caracteres inválidos: {parts[1]}");
            }

            string[] subdomains = parts[1].Split('.');
            if (subdomains.Where(d => !string.IsNullOrEmpty(d)).Count() < 2) // "logica basada en a.com => a y com (2)"
            {
                throw new ArgumentException($"La dirección de email configurada tiene un dominio inválido: {address}");
            }
            foreach (string subdomain in subdomains)
            {
                if (Regex.IsMatch(subdomain, " \t\n\r\0\x0B-")) 
                {
                    throw new ArgumentException($"La dirección de email configurada tiene un subdominios que terminan con caracteres inválidos: {address}");
                }

                if (!Regex.IsMatch(subdomain, "^[a-z0-9-]+$", RegexOptions.IgnoreCase))
                {
                    throw new ArgumentException($"La dirección de email configurada tiene un subdominios con caracteres inválidos: {address}");
                }
            }
        }
    }
}