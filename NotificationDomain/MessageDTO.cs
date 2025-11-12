using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationDomain
{
    public class MessageDTO
    {
        public string CompanyLogoLink { get; set; } = string.Empty;
        public string TwitterLink { get; set; } = string.Empty;
        public string FacebookLink { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string ReplyTo { get; set; } = string.Empty;
        public string CopyrightName { get; set; } = string.Empty;
        public string[] Cc { get; set; }
        public string[] EmailAddresses { get; set; }
        public string Filename { get; set; } = string.Empty;
        public List<Stream> AttachmentInCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string[] Bcc { get; set; }
        public string CopyrightYear { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        //public List<IFormFile> Attachments { get; set; }
        public bool Ispersonalized { get; set; }
        public List<ContactsModel> Contacts { get; set; } = new List<ContactsModel>();
        //public string[] GroupedContacts { get; set; } 
        public string ToContacts { get; set; } = string.Empty;
        public string ToOthers { get; set; } = string.Empty;
        public string ISOCode { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        public string EmailAddress { get; set; } = string.Empty;
        public string EmailDisplayName { get; set; } = string.Empty;

        //bulksms or hostedsms.
        public string GateWayToUse { get; set; } = string.Empty;

        public SenderSettingsDTO SenderSettings { get; set; } = new SenderSettingsDTO();
    }

    public class ContactsModel
    {
        public Guid ID { get; set; }
        public string Name { get; set; } = "System";
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public string otherInfo { get; set; } = string.Empty;  
    }

    public class SenderSettingsDTO
    {
        public string Domain { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
