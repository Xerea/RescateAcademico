using System.Net.Http.Headers;

namespace RescateAcademico.Services
{
    public class SmsSender
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsSender> _logger;

        public SmsSender(HttpClient httpClient, IConfiguration configuration, ILogger<SmsSender> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_configuration["TWILIO_ACCOUNT_SID"]) &&
            !string.IsNullOrWhiteSpace(_configuration["TWILIO_AUTH_TOKEN"]) &&
            !string.IsNullOrWhiteSpace(_configuration["TWILIO_FROM_NUMBER"]);

        public async Task<SmsSendResult> SendAsync(string phoneNumber, string message)
        {
            if (!IsConfigured)
            {
                return SmsSendResult.NotConfigured("El canal SMS no está configurado. Agrega TWILIO_ACCOUNT_SID, TWILIO_AUTH_TOKEN y TWILIO_FROM_NUMBER.");
            }

            var accountSid = _configuration["TWILIO_ACCOUNT_SID"]!;
            var authToken = _configuration["TWILIO_AUTH_TOKEN"]!;
            var fromNumber = _configuration["TWILIO_FROM_NUMBER"]!;
            var normalizedPhone = NormalizeMexicanPhone(phoneNumber);

            if (string.IsNullOrWhiteSpace(normalizedPhone))
            {
                return SmsSendResult.Failed("El teléfono del tutor legal no es válido.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.twilio.com/2010-04-01/Accounts/{Uri.EscapeDataString(accountSid)}/Messages.json");
            var authBytes = System.Text.Encoding.ASCII.GetBytes($"{accountSid}:{authToken}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["From"] = fromNumber,
                ["To"] = normalizedPhone,
                ["Body"] = message
            });

            try
            {
                using var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return SmsSendResult.Sent();
                }

                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Twilio SMS failed with {Status}: {Body}", response.StatusCode, body);
                return SmsSendResult.Failed("No se pudo enviar el SMS. Revisa la configuración del proveedor.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS");
                return SmsSendResult.Failed("No se pudo contactar al proveedor SMS.");
            }
        }

        private static string? NormalizeMexicanPhone(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return null;
            var trimmed = phoneNumber.Trim();
            if (trimmed.StartsWith("+")) return trimmed;

            var digits = new string(trimmed.Where(char.IsDigit).ToArray());
            if (digits.Length == 10) return $"+52{digits}";
            if (digits.Length == 12 && digits.StartsWith("52")) return $"+{digits}";
            return null;
        }
    }

    public record SmsSendResult(bool Success, bool Configured, string Message)
    {
        public static SmsSendResult Sent() => new(true, true, "SMS enviado al tutor legal.");
        public static SmsSendResult NotConfigured(string message) => new(false, false, message);
        public static SmsSendResult Failed(string message) => new(false, true, message);
    }
}
