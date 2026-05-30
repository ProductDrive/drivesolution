using NotificationDomain;
using PD.WhatsAppSender;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NotificationWorker
{
    public class WhatsAppEngine
    {
        public static bool SendWhatsApp(MessageDTO msgModel, WhatsAppCredentials? credentials = null)
        {
            try
            {
                if (msgModel.Contacts == null || !msgModel.Contacts.Any())
                {
                    Console.WriteLine("[WhatsApp] No contacts found");
                    return false;
                }

                bool allSent = true;
                foreach (var contact in msgModel.Contacts)
                {
                    if (string.IsNullOrWhiteSpace(contact.Phone))
                    {
                        Console.WriteLine($"[WhatsApp] Skipping contact {contact.Name} - no phone number");
                        allSent = false;
                        continue;
                    }

                    var messageForContact = new MessageDTO
                    {
                        MessageType = PDMessageType.WhatsApp,
                        Subject = msgModel.Subject,
                        Message = msgModel.Message,
                        EmailDisplayName = msgModel.EmailDisplayName,
                        Contacts = new System.Collections.Generic.List<ContactsModel> { contact },
                        SenderSettings = msgModel.SenderSettings,
                        FallBackSenderSettings = msgModel.FallBackSenderSettings
                    };

                    var result = WhatsAppHelper.SendWhatsAppDirect(messageForContact, credentials);
                    if (!result)
                    {
                        allSent = false;
                    }
                }

                return allSent;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WhatsApp] Error: {ex.Message}");
                return false;
            }
        }
    }
}
