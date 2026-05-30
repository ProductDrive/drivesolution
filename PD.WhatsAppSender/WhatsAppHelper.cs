using NotificationDomain;
using System;
using System.Linq;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace PD.WhatsAppSender
{
    public static class WhatsAppHelper
    {
        public static MessageDTO BuildWhatsAppMessage(BirthdaySubscription sub, string recipientPhoneNumber, string template = "")
        {
            var phoneNumber = NormalizePhoneNumber(recipientPhoneNumber);

            var message = new MessageDTO
            {
                MessageType = PDMessageType.WhatsApp,
                Subject = $"Birthday reminder: {sub.Name}",
                Message = string.IsNullOrWhiteSpace(template) 
                    ? $"Reminder: {sub.Name} has a birthday on {sub.BirthMonth}/{sub.BirthDay}." 
                    : template,
                Contacts = new System.Collections.Generic.List<ContactsModel>
                {
                    new ContactsModel { Phone = phoneNumber }
                },
                EmailDisplayName = $"{sub.Name} Birthday"
            };

            return message;
        }

        public static bool SendWhatsAppDirect(MessageDTO message, WhatsAppCredentials? credentials = null)
        {
            try
            {
                var credentialsToUse = credentials ?? new WhatsAppCredentials();
                var contact = message.Contacts?.FirstOrDefault();
                if (contact == null || string.IsNullOrWhiteSpace(contact.Phone))
                {
                    Console.WriteLine("[WhatsApp] Error: No valid phone number found");
                    return false;
                }

                var phoneNumber = NormalizePhoneNumber(contact.Phone);

                if (!string.IsNullOrWhiteSpace(phoneNumber))
                {
                    return SendViaTwilio(credentials, phoneNumber, message.Message);
                }

                Console.WriteLine($"[WhatsApp] Mock send to {phoneNumber}: {message.Message}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WhatsApp] Error sending message: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> SendWhatsAppAsync(MessageDTO message, WhatsAppCredentials? credentials = null)
        {
            return await Task.Run(() => SendWhatsAppDirect(message, credentials));
        }

        private static bool SendViaTwilio(WhatsAppCredentials credentials, string toNumber, string message)
        {
            try
            {
                credentials = credentials ?? new WhatsAppCredentials();

                TwilioClient.Init(credentials.AccountSid, credentials.AuthToken);

                var from = new PhoneNumber($"whatsapp:{credentials.FromNumber}");
                var to = new PhoneNumber($"whatsapp:{toNumber}");

                var messageResult = MessageResource.Create(
                    from: from,
                    to: to,
                    body: message
                );
                Console.WriteLine($"[WhatsApp] Twilio Message SID: {messageResult.Sid}");
                return messageResult.Status != MessageResource.StatusEnum.Failed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WhatsApp] Twilio Error: {ex.Message}");
                return false;
            }
        }

        private static string NormalizePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return string.Empty;

            return new string(phoneNumber.Where(c => char.IsDigit(c) || c == '+').ToArray());
        }
    }

    public class WhatsAppCredentials
    {
        public string AccountSid { get; set; }
        public string AuthToken { get; set; }
        public string FromNumber { get; set; }
    }
}
