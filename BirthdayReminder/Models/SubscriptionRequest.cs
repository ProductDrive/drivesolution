using System.Collections.Generic;

namespace BirthdayReminder.Models
{
    public class SubscriptionRequest
    {
        public string CelebrantId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int BirthDay { get; set; }
        public int BirthMonth { get; set; }
        public List<string> NotificationType { get; set; } = new();
        public List<string> NotifyTimes { get; set; } = new();
    }
}