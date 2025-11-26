using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationDomain;

namespace NotificationWorker
{

    public class NotificationConsumer : IConsumer<MessageDTO>
    {
        private readonly NotificationDbContext _db;
        public NotificationConsumer(NotificationDbContext db)
        {
            _db = db;
        }
        public async Task Consume(ConsumeContext<MessageDTO> context)
        {
            var record = new NotificationRecord
            {
                NotificationType = "Email",
                Recipient = context.Message.Contacts.Count>1? $"{context.Message.Contacts.First().Email} and others": $"{context.Message.Contacts.First().Email}",
                Subject = context.Message.Subject,
                SourceApp = "EmailApi",
                SentAt= DateTime.UtcNow,
            };

            _db.Notifications.Add(record);
            await _db.SaveChangesAsync();

            var messageObject = context.Message;
            Console.WriteLine($"📧 Sending email");

            if (messageObject.SenderSettings.OnBehalf)
            {
                messageObject.SenderSettings = messageObject.FallBackSenderSettings;
                var sent = EmailEngine.SendEmail(messageObject);
            }
            else
            {
                
            }


            await Task.CompletedTask;
        }
    }
}
