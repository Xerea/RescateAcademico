using System.Net;
using System.Net.Mail;

namespace RescateAcademico.Services;

public interface IEmailDeliveryService
{
    bool IsConfigured { get; }
    Task<bool> SendAsync(string recipient, string subject, string htmlBody);
}

public class SmtpEmailDeliveryService : IEmailDeliveryService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailDeliveryService> _logger;

    public SmtpEmailDeliveryService(IConfiguration configuration, ILogger<SmtpEmailDeliveryService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_configuration["Smtp:Host"])
        && !string.IsNullOrWhiteSpace(_configuration["Smtp:From"]);

    public async Task<bool> SendAsync(string recipient, string subject, string htmlBody)
    {
        if (!IsConfigured) return false;

        try
        {
            using var client = new SmtpClient(_configuration["Smtp:Host"]!, _configuration.GetValue("Smtp:Port", 587))
            {
                EnableSsl = _configuration.GetValue("Smtp:EnableSsl", true),
                Credentials = string.IsNullOrWhiteSpace(_configuration["Smtp:Username"])
                    ? CredentialCache.DefaultNetworkCredentials
                    : new NetworkCredential(_configuration["Smtp:Username"], _configuration["Smtp:Password"])
            };
            using var message = new MailMessage(_configuration["Smtp:From"]!, recipient, subject, htmlBody) { IsBodyHtml = true };
            await client.SendMailAsync(message);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo entregar correo a {Recipient}", recipient);
            return false;
        }
    }
}
