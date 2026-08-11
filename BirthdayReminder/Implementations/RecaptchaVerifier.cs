using System.Text;
using System.Text.Json;

namespace BirthdayReminder.Implementations
{
    public class RecaptchaVerifyResult
    {
        public bool Valid { get; set; }
        public double? Score { get; set; }
        public string? InvalidReason { get; set; }
    }

    /// <summary>
    /// Verifies reCAPTCHA Enterprise tokens via the Google Cloud assessments API.
    /// Config (env vars): RECAPTCHA_PROJECT_ID, RECAPTCHA_API_KEY, RECAPTCHA_SITE_KEY.
    /// </summary>
    public class RecaptchaVerifier
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly double _scoreThreshold;

        public RecaptchaVerifier(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            var threshold = configuration["RECAPTCHA_SCORE_THRESHOLD"];
            _scoreThreshold = double.TryParse(threshold, out var parsed) ? parsed : 0.5;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_configuration["RECAPTCHA_PROJECT_ID"]) &&
            !string.IsNullOrWhiteSpace(_configuration["RECAPTCHA_API_KEY"]) &&
            !string.IsNullOrWhiteSpace(_configuration["RECAPTCHA_SITE_KEY"]);

        public async Task<RecaptchaVerifyResult> VerifyAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return new RecaptchaVerifyResult { Valid = false, InvalidReason = "MISSING" };

            var projectId = _configuration["RECAPTCHA_PROJECT_ID"];
            var apiKey = _configuration["RECAPTCHA_API_KEY"];
            var siteKey = _configuration["RECAPTCHA_SITE_KEY"];

            var url = $"https://recaptchaenterprise.googleapis.com/v1/projects/{projectId}/assessments?key={apiKey}";

            var body = new
            {
                @event = new
                {
                    token,
                    siteKey,
                    expectedAction = "submit"
                }
            };

            using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return new RecaptchaVerifyResult { Valid = false, InvalidReason = $"API_ERROR_{(int)response.StatusCode}" };

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            var tokenProps = root.TryGetProperty("tokenProperties", out var tp) ? tp : default;
            var valid = tokenProps.ValueKind != JsonValueKind.Undefined &&
                        tokenProps.TryGetProperty("valid", out var v) &&
                        v.ValueKind == JsonValueKind.True;
            var invalidReason = tokenProps.TryGetProperty("invalidReason", out var ir) ? ir.GetString() : null;

            double? score = null;
            if (root.TryGetProperty("riskAnalysis", out var risk) &&
                risk.TryGetProperty("score", out var s) &&
                s.ValueKind == JsonValueKind.Number)
            {
                score = s.GetDouble();
            }

            var ok = valid && (!score.HasValue || score.Value >= _scoreThreshold);
            return new RecaptchaVerifyResult { Valid = ok, Score = score, InvalidReason = invalidReason };
        }
    }
}
