namespace BirthdayReminder.Models
{
    public class OtpRequest { }

    public class OtpVerifyRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class OtpResponse
    {
        public string SessionId { get; set; } = string.Empty;
    }

    public class OtpVerifyResponse
    {
        public string Token { get; set; } = string.Empty;
    }

    public class WhatsAppReminderResponse
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string WhatsappNumber { get; set; } = string.Empty;
        public List<CelebrantReminder> Celebrants { get; set; } = new();
    }

    public class CelebrantReminder
    {
        public string CelebrantId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int BirthDay { get; set; }
        public int BirthMonth { get; set; }
        public string NotifyTime { get; set; } = string.Empty;
        public int DaysUntilBirthday { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
