using BirthdayReminder.Models;
using PD.EmailSender.Helpers;
using PD.EmailSender.Helpers.Model;

namespace BirthdayReminder.Implementations
{
    public interface IDeletionRequestService
    {
        Task SendDeletionRequestEmailAsync(DeletionRequest request);
    }

    public class DeletionRequestService : IDeletionRequestService
    {
        private const string AdminEmail1 = "afeexclusive@gmail.com";
        private const string AdminEmail2 = "vegiorder@gmail.com";

        public async Task SendDeletionRequestEmailAsync(DeletionRequest request)
        {
            var subject = $"Account Deletion Request — {request.Email}";
            var htmlBody = BuildEmailBody(request);

            var recipients = new List<string> { AdminEmail1, AdminEmail2 };

            foreach (var recipient in recipients)
            {
                try
                {
                    var messageDto = new MessageModel
                    {
                        Contacts = new List<ContactsModel>
                        {
                            new ContactsModel { Email = recipient }
                        },
                        Subject = subject,
                        Message = htmlBody,
                        SenderSettings = new SenderSettingsDTO { OnBehalf = true },
                        FallBackSenderSettings = new SenderSettingsDTO { OnBehalf = true },
                        EmailDisplayName = "BirthdayAlert Account Deletion"
                    };

                    await SendMailVTwo.SendSingleEmailOnBehalf(messageDto);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send deletion request email to {recipient}: {ex.Message}");
                }
            }
        }

        private string BuildEmailBody(DeletionRequest request)
        {
            var reason = string.IsNullOrWhiteSpace(request.Reason)
                ? "<em>No reason provided</em>"
                : $"<p>\"{System.Net.WebUtility.HtmlEncode(request.Reason)}\"</p>";

            return $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: Arial, sans-serif; color: #333; line-height: 1.6; padding: 20px; }}
    .container {{ max-width: 600px; margin: 0 auto; background: #f9f9f9; border-radius: 12px; padding: 30px; border: 1px solid #e5e7eb; }}
    h2 {{ color: #dc2626; margin-top: 0; }}
    .detail {{ background: white; border-radius: 8px; padding: 16px; margin: 16px 0; border: 1px solid #e5e7eb; }}
    .label {{ font-weight: 600; color: #6b7280; font-size: 0.85em; text-transform: uppercase; letter-spacing: 0.05em; }}
    .value {{ margin-top: 4px; color: #111827; }}
    .footer {{ margin-top: 24px; padding-top: 16px; border-top: 1px solid #e5e7eb; font-size: 0.85em; color: #9ca3af; }}
  </style>
</head>
<body>
  <div class='container'>
    <h2>&#9888;&#65039; Account Deletion Request</h2>
    <p>A user has requested deletion of their BirthdayAlert account.</p>

    <div class='detail'>
      <div class='label'>User Email</div>
      <div class='value'>{System.Net.WebUtility.HtmlEncode(request.Email)}</div>
    </div>

    <div class='detail'>
      <div class='label'>Firebase UID</div>
      <div class='value'><code>{System.Net.WebUtility.HtmlEncode(request.UserId)}</code></div>
    </div>

    <div class='detail'>
      <div class='label'>Request Date (UTC)</div>
      <div class='value'>{DateTime.UtcNow:dd MMMM yyyy HH:mm} UTC</div>
    </div>

    <div class='detail'>
      <div class='label'>Reason</div>
      <div class='value'>{reason}</div>
    </div>

    <div class='footer'>
      <p>This is an automated notification from BirthdayAlert. Please process this deletion request in accordance with your data protection obligations.</p>
    </div>
  </div>
</body>
</html>";
        }
    }
}
