using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationDomain;
using PD.EmailSender.Helpers;
using static PD.EmailSender.Helpers.SendMail;

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
                Recipient = context.Message.ToContacts,
                Subject = context.Message.Subject,
                SourceApp = "EmailApi",
                SentAt= DateTime.UtcNow,
            };

            _db.Notifications.Add(record);
            await _db.SaveChangesAsync();

            var messageObject = context.Message;
            Console.WriteLine($"📧 Sending email to {messageObject.ToContacts}: {messageObject.Subject}");

            //var (isAuth, mySenderSettings) = await SendMail.AuthenticateSenderDomain("aafe@projectdriveng.com.ng", "");
            //var (isAuth, mySenderSettings) = await SendMail.AuthenticateSenderDomain("afee@productdrive.com.ng", "");
            //if (isAuth)
            //{
            //    messageObject.SenderSettings.Email = mySenderSettings.Email;
            //    messageObject.SenderSettings.Port = mySenderSettings.Port;
            //    messageObject.SenderSettings.Password = mySenderSettings.Password;
            //    messageObject.SenderSettings.Domain = mySenderSettings.Domain;
            //}

            var getRecord = Decoderr.GetMailAccounts().FirstOrDefault(x => x.Email == "aafe@projectdriveng.com.ng");
            if (getRecord != null)
            {
                messageObject.SenderSettings.Domain = getRecord.Domain;
                messageObject.SenderSettings.Password = Decoderr.DecryptPassword(getRecord.Password);
                messageObject.SenderSettings.Email = getRecord.Email;
                messageObject.SenderSettings.Port = getRecord.Port;

            }

            bool res = SendMail.SendSingleEmail(
                new PD.EmailSender.Helpers.Model.MessageModel()
                {
                    AttachmentInCode = messageObject.AttachmentInCode,
                    Bcc = messageObject.Bcc,
                    Cc = messageObject.Cc,
                    CompanyLogoLink = messageObject.CompanyLogoLink,
                    CopyrightName = messageObject.CopyrightName,
                    CopyrightYear = messageObject.CopyrightYear,
                    EmailAddresses = messageObject.EmailAddresses,
                    EmailDisplayName = messageObject.EmailDisplayName,
                    FacebookLink = messageObject.FacebookLink,
                    Filename = messageObject.Filename,
                    Message = messageObject.Message,
                    ReplyTo = messageObject.ReplyTo,
                    Subject = messageObject.Subject,
                    TwitterLink = messageObject.TwitterLink,
                    User = messageObject.User
                },
                new SenderSettings
                {
                    Email = messageObject.SenderSettings.Email,
                    Port = messageObject.SenderSettings.Port,
                    Password = messageObject.SenderSettings.Password,
                    Domain = messageObject.SenderSettings.Domain
                }
            );
            if (res)
            {
                Console.WriteLine($"📧 Email sent to {messageObject.ToContacts}: {messageObject.Subject}");
            }

            await Task.CompletedTask;
        }
    }
}
