using NotificationDomain;
using System.Collections.Generic;

namespace BirthdayReminder.Models
{
    public class SubscriptionRequest
    {
        public string CelebrantId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int BirthDay { get; set; }
        public int BirthMonth { get; set; }
        public List<NotificationType> NotificationTypes { get; set; } = new();
        public List<NotifyTime> NotifyTimes { get; set; } = new();
        public string UserId { get; set; } = string.Empty;
    }
}

