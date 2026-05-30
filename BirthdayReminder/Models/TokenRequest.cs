namespace BirthdayReminder.Models
{
    public class RegisterTokenRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string Platform { get; set; } = "web";
    }

    public class UnregisterTokenRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }
}
