using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationDomain
{
    public class NotificationRecord
    {
        public int Id { get; set; }
        public string NotificationType { get; set; } = default!; // "email" or "sms"
        public string Exception { get; set; } = default!;
        public string Recipient { get; set; } = default!;
        public string Subject { get; set; } = default!;
        public string SourceApp { get; set; } = default!; // App that published
        public DateTime SentAt { get; set; }
    }
}
