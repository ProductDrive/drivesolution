using BirthdayReminder.Data;
using BirthdayReminder.interfaces;
using BirthdayReminder.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NotificationDomain;
using PD.EmailSender.Helpers;
using PD.EmailSender.Helpers.Model;
using PD.WhatsAppSender;
using System.Collections.Generic;
using System.Text;
using static MassTransit.Monitoring.Performance.BuiltInCounters;

namespace BirthdayReminder.Implementations
{
    public interface ISubscriptionNotificationService
    {
        Task SendSubscriptionNotificationsAsync();
    }

    public class SubscriptionNotificationService : ISubscriptionNotificationService
    {
        private readonly NotificationDbContext _dbContext;
        private readonly IFirebaseStoreService _firebaseStoreService;
        private readonly IPushNotificationService _pushNotificationService;
        public List<UserRecord> _allUsers { get; set; }

        public SubscriptionNotificationService(
            NotificationDbContext dbContext,
            IFirebaseStoreService firebaseStoreService,
            IPushNotificationService pushNotificationService)
        {
            _dbContext = dbContext;
            _firebaseStoreService = firebaseStoreService;
            _pushNotificationService = pushNotificationService;
            _allUsers = _firebaseStoreService.GetAllUsers().Result;
        }

        // Call all firebase queries first, then filter the subscriptions in-memory to minimize database calls and optimize performance
       

        public async Task SendSubscriptionNotificationsAsync()
        {

            var subscriptions = await _dbContext.BirthdaySubscriptions
                .ToListAsync();

            foreach (var sub in subscriptions)
            {
                Console.WriteLine(sub.Name);
            }

            await SendEmailNotificationsAsync(subscriptions);
            await SendWhatsAppNotificationsAsync(subscriptions);
            await SendPushNotificationsAsync(subscriptions);
        }

        private async Task SendEmailNotificationsAsync(List<BirthdaySubscription> allSubscriptions)
        {
            var emailSubscriptions = allSubscriptions
                .Where(x => x.NotificationTypes.Contains(NotificationType.Email))
                .ToList();

            var subscriptionsToNotify = emailSubscriptions
                .SelectMany(s => FilterSubcriptions(s)
                    .Select(d => new { Subscription = s, NotifyTime = d.NotifyTime }))
                .ToList();

            if (subscriptionsToNotify.Count == 0)
            {
                Console.WriteLine("No email notifications to send today.");
                return;
            }

            var subscriptionsByEmail = subscriptionsToNotify
                .Select(item => new 
                { 
                    Email = GetUserEmailForCelebrant(item.Subscription.UserId),
                    item.Subscription,
                    item.NotifyTime
                })
                .Where(x => !string.IsNullOrEmpty(x.Email))
                .GroupBy(x => x.Email)
                .ToList();

            foreach (var group in subscriptionsByEmail)
            {
                var displayName = new List<string>();
                if (string.IsNullOrEmpty(group.Key))
                {
                    continue;
                }
                var email = group.Key;
                var celebrants = group.Select(g => new NotificationTriger
                { 
                    Name = g.Subscription.Name, 
                    NotifyTime = g.NotifyTime 
                }).ToList();

                var plural = celebrants.Count > 1 ? "s" : "";
                var subject = $"Reminder: {celebrants.Count} Birthday{plural} Coming Up Soon";
                displayName.AddRange(celebrants.Select(x => x.Name));
                var emailDisplayName = string.Empty;
                if (displayName.Count > 1)
                {
                    emailDisplayName = $"{String.Join(',', displayName.Take(2))}... Birthdays";
                }
                if (displayName.Count == 1)
                {
                    emailDisplayName = $"{displayName[0]}'s Birthday";
                }

                var message = BuildMultiCelebrantMessage(celebrants);

                await SendEmailAsync(email, subject, message, emailDisplayName);
            }
        }

        private async Task SendWhatsAppNotificationsAsync(List<BirthdaySubscription> allSubscriptions)
        {
            var whatsappSubscriptions = allSubscriptions
                .Where(x => x.NotificationTypes.Contains(NotificationType.WhatsApp))
                .ToList();

            var subscriptionsToNotify = whatsappSubscriptions
                .SelectMany(s => FilterSubcriptions(s)
                    .Select(d => new { Subscription = s, NotifyTime = d.NotifyTime }))
                .ToList();

            if (subscriptionsToNotify.Count == 0)
            {
                Console.WriteLine("No WhatsApp notifications to send today.");
                return;
            }

            var subscriptionsByPhone = subscriptionsToNotify
                .Select(item => new 
                { 
                    Phone = GetUserPhoneForCelebrant(item.Subscription.UserId),
                    item.Subscription,
                    item.NotifyTime
                })
                .Where(x => !string.IsNullOrEmpty(x.Phone))
                .GroupBy(x => x.Phone)
                .ToList();

            foreach (var group in subscriptionsByPhone)
            {
                if (string.IsNullOrEmpty(group.Key))
                {
                    continue;
                }
                var phoneNumber = group.Key;
                var celebrants = group.Select(g => new NotificationTriger
                { 
                    Name = g.Subscription.Name, 
                    NotifyTime = g.NotifyTime 
                }).ToList();

                var message = BuildWhatsAppMessage(celebrants);

                await SendWhatsAppAsync(phoneNumber, message);
            }
        }

        private async Task SendPushNotificationsAsync(List<BirthdaySubscription> allSubscriptions)
        {
            var pushSubscriptions = allSubscriptions
                .Where(x => x.NotificationTypes.Contains(NotificationType.Push))
                .ToList();

            var subscriptionsToNotify = pushSubscriptions
                .SelectMany(s => FilterSubcriptions(s)
                    .Select(d => new { Subscription = s, NotifyTime = d.NotifyTime }))
                .ToList();

            if (subscriptionsToNotify.Count == 0)
            {
                Console.WriteLine("No push notifications to send today.");
                return;
            }

            var subscriptionsByUser = subscriptionsToNotify
                .GroupBy(x => x.Subscription.UserId)
                .ToList();

            foreach (var group in subscriptionsByUser)
            {
                if (string.IsNullOrEmpty(group.Key)) continue;

                var userId = group.Key;
                var celebrants = group.Select(g => new NotificationTriger
                {
                    Name = g.Subscription.Name,
                    NotifyTime = g.NotifyTime
                }).ToList();

                var plural = celebrants.Count > 1 ? "s" : "";
                var title = "Birthday Reminder";
                var body = celebrants.Count == 1
                    ? $"{celebrants[0].Name}'s birthday is coming up {FormatNotifyTime(celebrants[0].NotifyTime)}"
                    : $"{celebrants.Count} birthday{plural} coming up soon!";

                await _pushNotificationService.SendToUserAsync(userId, title, body);
            }
        }

        private static string FormatNotifyTime(NotifyTime notifyTime)
        {
            return notifyTime switch
            {
                NotifyTime.OneMonthBefore => "in 1 month",
                NotifyTime.TwoWeeksBefore => "in 2 weeks",
                NotifyTime.ThreeDaysBefore => "in 3 days",
                _ => "soon"
            };
        }

        private string GetUserEmailForCelebrant(string userId)
        {
            if (userId != null)
            {
                var user = _allUsers.FirstOrDefault(u => u.UserId == userId);
                if (user != null)
                {
                    return user.Email;
                }
                else
                {
                    Console.WriteLine($"UserId {userId} not found in user records.");
                    return string.Empty;
                }
            }
            else            
            {
                Console.WriteLine("UserId is null for a subscription.");
                return string.Empty;
            }
        }

        private string GetUserPhoneForCelebrant(string userId)
        {
            if (userId != null)
            {
                var user = _allUsers.FirstOrDefault(u => u.UserId == userId);
                if (user != null && !string.IsNullOrWhiteSpace(user.WhatsappNumber))
                {
                    return user.WhatsappNumber;
                }
                else
                {
                    Console.WriteLine($"UserId {userId} not found in user records or no phone number.");
                    return string.Empty;
                }
            }
            else            
            {
                Console.WriteLine("UserId is null for a subscription.");
                return string.Empty;
            }
        }

        private string BuildMultiCelebrantMessage(List<NotificationTriger> celebrants)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<h2>Upcoming Birthdays</h2>");
            sb.AppendLine("<ul>");
            foreach (var celebrant in celebrants)
            {
                var timeDescription = celebrant.NotifyTime switch
                {
                    NotifyTime.OneMonthBefore => "in 1 month",
                    NotifyTime.TwoWeeksBefore => "in 2 weeks",
                    NotifyTime.ThreeDaysBefore => "in 3 days",
                    _ => "soon"
                };
                sb.AppendLine($"<li><strong>{celebrant.Name}</strong>'s birthday is coming up {timeDescription}</li>");
            }
            sb.AppendLine("</ul>");
            sb.AppendLine("<p>Don't forget to send your wishes!</p>");
            sb.AppendLine("<p><a href='https://drive-birthday-reminder.vercel.app/auth'>Birthday Reminder App</a></p>");

            return sb.ToString();
        }

        private List<NotificationTriger> FilterSubcriptions(BirthdaySubscription subscription)
        {
            var today = DateTime.Today;
            var result = new List<NotificationTriger>();
            var birthDateThisYear = new DateTime(today.Year, subscription.BirthMonth, subscription.BirthDay);

            if (subscription.NotifyTimes.Contains(NotifyTime.OneMonthBefore))
            {
                if (today.AddMonths(1) == birthDateThisYear)
                {
                    result.Add(new NotificationTriger {NotifyTime= NotifyTime.OneMonthBefore });
                }
            }

            if (subscription.NotifyTimes.Contains(NotifyTime.TwoWeeksBefore))
            {
                if (today.AddDays(14) == birthDateThisYear)
                    result.Add(new NotificationTriger {NotifyTime = NotifyTime.TwoWeeksBefore });
            }

            if (subscription.NotifyTimes.Contains(NotifyTime.ThreeDaysBefore))
            {
                if (today.AddDays(3) == birthDateThisYear)
                    result.Add(new NotificationTriger {NotifyTime = NotifyTime.ThreeDaysBefore });
            }

            return result;
        }


        private async Task SendEmailAsync(string toEmail, string subject, string message, string emailDisplayName)
        {
            try
            {
                var messageDto = new PD.EmailSender.Helpers.Model.MessageModel
                {
                    Contacts = new List<PD.EmailSender.Helpers.Model.ContactsModel> { new PD.EmailSender.Helpers.Model.ContactsModel { Email = toEmail } },
                    Subject = subject,
                    Message = message,
                    SenderSettings = new PD.EmailSender.Helpers.Model.SenderSettingsDTO { OnBehalf = true },
                    FallBackSenderSettings = new PD.EmailSender.Helpers.Model.SenderSettingsDTO { OnBehalf = true },
                    EmailDisplayName = emailDisplayName
                };

                await SendMailVTwo.SendSingleEmailOnBehalf(messageDto);
                Console.WriteLine($"Email sent to {toEmail} for subject: {subject}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email to {toEmail}: {ex.Message}");
            }
        }

        private string BuildWhatsAppMessage(List<NotificationTriger> celebrants)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"🎂 *Upcoming Birthdays*");
            sb.AppendLine();
            foreach (var celebrant in celebrants)
            {
                var timeDescription = celebrant.NotifyTime switch
                {
                    NotifyTime.OneMonthBefore => "in 1 month",
                    NotifyTime.TwoWeeksBefore => "in 2 weeks",
                    NotifyTime.ThreeDaysBefore => "in 3 days",
                    _ => "soon"
                };
                sb.AppendLine($"• *{celebrant.Name}*'s birthday is coming up {timeDescription}");
            }
            sb.AppendLine();
            sb.AppendLine("Don't forget to send your wishes!");

            return sb.ToString();
        }

        private async Task SendWhatsAppAsync(string phoneNumber, string message)
        {
            try
            {
                var messageDto = new NotificationDomain.MessageDTO
                {
                    MessageType = NotificationDomain.PDMessageType.WhatsApp,
                    Subject = "Birthday Reminder",
                    Message = message,
                    Contacts = new List<NotificationDomain.ContactsModel>
                    {
                        new NotificationDomain.ContactsModel { Phone = phoneNumber }
                    },
                    SenderSettings = new NotificationDomain.SenderSettingsDTO { OnBehalf = true },
                    FallBackSenderSettings = new NotificationDomain.SenderSettingsDTO { OnBehalf = true }
                };

                var result = PD.WhatsAppSender.WhatsAppHelper.SendWhatsAppDirect(messageDto);
                if (result)
                {
                    Console.WriteLine($"WhatsApp sent to {phoneNumber}");
                }
                else
                {
                    Console.WriteLine($"Failed to send WhatsApp to {phoneNumber}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send WhatsApp to {phoneNumber}: {ex.Message}");
            }
            finally
            {
                await Task.CompletedTask; // Placeholder for any asynchronous cleanup if needed in the future
            }
        }
    }
}
