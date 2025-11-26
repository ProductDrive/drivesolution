using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationDomain
{
    public class MessageDTO
    {
        public string ReplyTo { get; set; } = string.Empty;
        public List<AttachmentDTO>? Attachments { get; set; }
        public string[] Cc { get; set; }
        public string Message { get; set; } = string.Empty;
        public string[] Bcc { get; set; }
        public string Subject { get; set; } = string.Empty;
        public bool Ispersonalized { get; set; }
        public List<ContactsModel> Contacts { get; set; } = new List<ContactsModel>();
        public string ISOCode { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        public string EmailDisplayName { get; set; } = string.Empty;

        //bulksms or hostedsms.
        public string GateWayToUse { get; set; } = string.Empty;

        //sms, whatsapp, email
        public PDMessageType MessageType { get; set; } = PDMessageType.Email;

        public SenderSettingsDTO SenderSettings { get; set; } = new SenderSettingsDTO();
        public SenderSettingsDTO FallBackSenderSettings { get; set; } = new SenderSettingsDTO();
    }

    public class ContactsModel
    {
        public Guid ID { get; set; }
        public string Name { get; set; } = "info";
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public string otherInfo { get; set; } = string.Empty;  
    }

    public class SenderSettingsDTO
    {
        public bool OnBehalf { get; set; }
        public string Domain { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
    }

    public class AttachmentDTO
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public byte[]? FileBytes { get; set; }
    }

    public enum PDMessageType
    {
        Email,
        SMS,
        WhatsApp
    }
}
