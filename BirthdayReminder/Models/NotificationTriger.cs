using NotificationDomain;

namespace BirthdayReminder.Models
{
    public class NotificationTriger
    {
        public string Name { get; set; } = string.Empty;
        public NotifyTime NotifyTime { get; set; }
        public DateTime NotifyDate { get; set; }
    }
}
