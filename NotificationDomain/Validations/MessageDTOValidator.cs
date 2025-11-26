using NotificationDomain;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;

public static class MessageDTOValidator
{
    public static ValidationResult Validate(MessageDTO message)
    {
        var result = new ValidationResult();

        if (message == null)
        {
            result.Errors.Add("Message payload cannot be null.");
            return result;
        }

        // Validate Subject
        if (string.IsNullOrWhiteSpace(message.Subject))
            result.Errors.Add("Subject is required.");

        // Validate Body/Message
        if (string.IsNullOrWhiteSpace(message.Message))
            result.Errors.Add("Message body cannot be empty.");

        // Validate Email Recipients
        if ((message.Contacts == null || message.Contacts.Count == 0))
        {
            result.Errors.Add("At least one recipient email is required.");
        }

        // Validate email formats
        if (message.Contacts != null && message.Contacts.Count > 0)
        {
            foreach (var contact in message.Contacts)
            {
                if (!EmailHelper.IsValidEmail(contact.Email))
                    result.Errors.Add($"Invalid recepient email address: {contact.Email}");
            }
        }



        // Validate Sender Settings
        var senderValidation = SenderSettingsValidator.Validate(message.SenderSettings);
        if (!senderValidation.IsValid)
            result.Errors.AddRange(senderValidation.Errors);


        // Validate ReplyTo
        if (string.IsNullOrEmpty(message.ReplyTo))
        {
            if (senderValidation.IsValid)
            {
                message.ReplyTo = message.SenderSettings.Email;
            }
        }
        else
        {
            if (!EmailHelper.IsValidEmail(message.ReplyTo) && senderValidation.IsValid)
            {
                message.ReplyTo = message.SenderSettings.Email;
            }
        }
       

        message.Bcc = ValidateCopies(message.Bcc);
        message.Cc = ValidateCopies(message.Cc);


        // Validate Attachments
        if (message.Attachments != null)
        {
            foreach (var a in message.Attachments)
            {
                var attachmentValidation = AttachmentValidator.Validate(a);
                if (!attachmentValidation.IsValid)
                    result.Errors.AddRange(attachmentValidation.Errors);
            }
        }

        return result;
    }

    private static string[] ValidateCopies(string[] addresses)
    {
        var tempBcc = new List<string>();
        if (addresses != null && addresses.Length > 0)
        {

            foreach (var item in addresses)
            {
                if (!EmailHelper.IsValidEmail(item))
                {
                    continue;
                }
                tempBcc.Add(item);
            }
            if (tempBcc.Count > 0)
            {
                addresses = tempBcc.ToArray();
            }
            else
            {
                addresses = null;
            }
            
        }
        return addresses;
    }
}
