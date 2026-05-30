using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationDomain;
using PD.WhatsAppSender;

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
            var messageObject = context.Message;

            if (messageObject.MessageType == PDMessageType.WhatsApp)
            {
                await HandleWhatsAppMessage(context);
            }
            else
            {
                await HandleEmailMessage(context);
            }
        }

        private async Task HandleEmailMessage(ConsumeContext<MessageDTO> context)
        {
            try
            {
                var messageObject = context.Message;
                Console.WriteLine($"📧 Sending email");

                if (messageObject.SenderSettings.OnBehalf)
                {
                    messageObject.SenderSettings = messageObject.FallBackSenderSettings;
                }
                
                var sent = EmailEngine.SendEmail(messageObject);

                var record = new NotificationRecord
                {
                    NotificationType = "Email",
                    Exception = sent ? "Success" : "Failed",
                    Recipient = messageObject.Contacts.Count > 1 
                        ? $"{messageObject.Contacts.First().Email} and others" 
                        : $"{messageObject.Contacts.First().Email}",
                    Subject = messageObject.Subject,
                    SourceApp = "EmailApi",
                    SentAt = DateTime.UtcNow,
                };
                _db.Notifications.Add(record);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var record = new NotificationRecord
                {
                    NotificationType = "Email",
                    Exception = ex.InnerException?.Message ?? ex.Message,
                    Recipient = context.Message.Contacts.Count > 1 
                        ? $"{context.Message.Contacts.First().Email} and others" 
                        : $"{context.Message.Contacts.First().Email}",
                    Subject = context.Message.Subject,
                    SourceApp = "EmailApi",
                    SentAt = DateTime.UtcNow,
                };
                _db.Notifications.Add(record);
                await _db.SaveChangesAsync();
            }
        }

        private async Task HandleWhatsAppMessage(ConsumeContext<MessageDTO> context)
        {
            try
            {
                var messageObject = context.Message;
                Console.WriteLine($"📱 Sending WhatsApp");

                var credentials = new WhatsAppCredentials
                {
                    AccountSid = messageObject.SenderSettings.Domain,
                    AuthToken = messageObject.SenderSettings.Password,
                    FromNumber = messageObject.SenderSettings.Email
                };

                var sent = WhatsAppEngine.SendWhatsApp(messageObject, credentials);

                var contact = messageObject.Contacts?.FirstOrDefault();
                var record = new NotificationRecord
                {
                    NotificationType = "WhatsApp",
                    Exception = sent ? "Success" : "Failed",
                    Recipient = contact?.Phone ?? "Unknown",
                    Subject = messageObject.Subject,
                    SourceApp = "WhatsAppApi",
                    SentAt = DateTime.UtcNow,
                };
                _db.Notifications.Add(record);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var record = new NotificationRecord
                {
                    NotificationType = "WhatsApp",
                    Exception = ex.InnerException?.Message ?? ex.Message,
                    Recipient = context.Message.Contacts?.FirstOrDefault()?.Phone ?? "Unknown",
                    Subject = context.Message.Subject,
                    SourceApp = "WhatsAppApi",
                    SentAt = DateTime.UtcNow,
                };
                _db.Notifications.Add(record);
                await _db.SaveChangesAsync();
            }
        }
    }
}
