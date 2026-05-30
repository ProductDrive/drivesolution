using NotificationDomain;

namespace WhatsAppApi.Helpers
{
    public class WhatsAppValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public static class WhatsAppMessageValidator
    {
        public static WhatsAppValidationResult Validate(MessageDTO message)
        {
            var result = new WhatsAppValidationResult();

            if (message == null)
            {
                result.IsValid = false;
                result.Errors.Add("Message cannot be null");
                return result;
            }

            if (message.Contacts == null || !message.Contacts.Any())
            {
                result.IsValid = false;
                result.Errors.Add("At least one contact is required");
                return result;
            }

            foreach (var contact in message.Contacts)
            {
                if (string.IsNullOrWhiteSpace(contact.Phone))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Contact {contact.Name} must have a phone number");
                }
            }

            if (string.IsNullOrWhiteSpace(message.Message))
            {
                result.IsValid = false;
                result.Errors.Add("Message content is required");
            }

            if (result.Errors.Count == 0)
            {
                result.IsValid = true;
            }

            return result;
        }
    }
}
