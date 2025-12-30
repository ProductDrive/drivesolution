namespace BirthdayReminder.Models
{
    public class FirebaseBirthdayStore
    {
        public static string ApiKey { get; set; }
        public static string ProjectId => "afebdayrem";
        public static string AppId { get; set; }
        public static string StorageBucket { get; set; }
        public static string AuthDomain { get; set; }
        public static string MessagingSenderId { get; set; }
        public static string CredentialsPath => "afebdayrem-firebase-adminsdk-fbsvc-83c5702430.json";
    }
}
