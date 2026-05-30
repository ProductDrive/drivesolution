namespace WhatsAppApi.Helpers
{
    public class WhatsAppSettings
    {
        public string GateWayToUse { get; set; } = "twilio";
        public string DefaultSenderPhone { get; set; } = string.Empty;
        public string TwilioAccountSid { get; set; } = string.Empty;
        public string TwilioAuthToken { get; set; } = string.Empty;
        public string TwilioWhatsAppNumber { get; set; } = string.Empty;
    }
}
