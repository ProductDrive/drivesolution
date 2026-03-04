using NotificationDomain;
using System;
using System.Linq;

namespace PD.WhatsAppSender
{
    // Reusable small helper for creating (and eventually sending) WhatsApp messages.
    public static class WhatsAppHelper
    {
        public static MessageDTO BuildWhatsAppMessage(BirthdaySubscription sub, string recipientIdentifier, string template = "")
        {
            var message = new MessageDTO
            {
                MessageType = PDMessageType.WhatsApp,
                Subject = $"Birthday reminder: {sub.Name}",
                Message = string.IsNullOrWhiteSpace(template) ? $"Reminder: {sub.Name} has a birthday on {sub.BirthMonth}/{sub.BirthDay}." : template,
                Contacts = new System.Collections.Generic.List<ContactsModel>
                {
                    new ContactsModel { Email = recipientIdentifier, Phone = recipientIdentifier }
                },
                EmailDisplayName = $"{sub.Name} Birthday"
            };

            return message;
        }

        public static bool SendWhatsAppDirect(MessageDTO m)
        {
            Console.WriteLine($"[WhatsApp] Sending to {m.Contacts?.FirstOrDefault()?.Phone ?? m.Contacts?.FirstOrDefault()?.Email}: {m.Message}");
            return true;
        }
    }
}
