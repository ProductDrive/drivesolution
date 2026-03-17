using BirthdayReminder.Data;
using BirthdayReminder.interfaces;
using BirthdayReminder.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NotificationDomain;
using PD.EmailSender.Helpers;
using PD.EmailSender.Helpers.Model;
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
        public List<UserRecord> _allUsers { get; set; }

        public SubscriptionNotificationService(
            NotificationDbContext dbContext,
            IFirebaseStoreService firebaseStoreService)
        {
            _dbContext = dbContext;
            _firebaseStoreService = firebaseStoreService;
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

            //var subscriptions = JsonConvert.DeserializeObject<List<BirthdaySubscription>>("[\n  {\n    \"Id\": \"38731314-024a-4487-bb9b-03dab3579df0\",\n    \"CelebrantId\": \"XAwk6gHPowaQqSsJMyTt\",\n    \"Name\": \"Julius \",\n    \"BirthDay\": \"16\",\n    \"BirthMonth\": \"03\",\n    \"CreatedAt\": \"2026-03-03\",\n    \"NotificationTypesJson\": \"[0]\",\n    \"NotifyTimesJson\": \"[1]\",\n    \"UserId\": \"2lhwqMIlNWOGyeQsc8EkxABZpTm1\"\n  },\n  {\n    \"Id\": \"790b5105-61b1-4bfe-98dc-c6e93e335dbe\",\n    \"CelebrantId\": \"StyQ3HUCtEpfs4f5iCAn\",\n    \"Name\": \"Ayokunle Afe\",\n    \"BirthDay\": \"13\",\n    \"BirthMonth\": \"4\",\n    \"CreatedAt\": \"2026-03-03\",\n    \"NotificationTypesJson\": \"[0,1]\",\n    \"NotifyTimesJson\": \"[0,1,2]\",\n    \"UserId\": \"2lhwqMIlNWOGyeQsc8EkxABZpTm1\"\n  },\n  {\n    \"Id\": \"83d138dc-a26d-4332-8df6-2adea78bdf34\",\n    \"CelebrantId\": \"veKlo061sZOtjmgaWRSF\",\n    \"Name\": \"Oloruko gigunnimi forthesakeoftestjare\",\n    \"BirthDay\": \"8\",\n    \"BirthMonth\": \"3\",\n    \"CreatedAt\": \"2026-03-03\",\n    \"NotificationTypesJson\": \"[0,1]\",\n    \"NotifyTimesJson\": \"[0,1,2]\",\n    \"UserId\": \"LBrpxQ9Mt5SirJMKpVgo8ZWD2re2\"\n  },\n  {\n    \"Id\": \"ad27b1a7-589a-4cf1-912d-4d21d605b41d\",\n    \"CelebrantId\": \"E2AuFrxadyWNxr1pfZNg\",\n    \"Name\": \"Testy Test\",\n    \"BirthDay\": \"16\",\n    \"BirthMonth\": \"3\",\n    \"CreatedAt\": \"2026-03-03\",\n    \"NotificationTypesJson\": \"[0,1]\",\n    \"NotifyTimesJson\": \"[0,1,2]\",\n    \"UserId\": \"LBrpxQ9Mt5SirJMKpVgo8ZWD2re2\"\n  },\n  {\n    \"Id\": \"ba1009ad-1afb-4f46-b019-3da2abcfa4a1\",\n    \"CelebrantId\": \"66W8gJGeayBhAqRW5KvF\",\n    \"Name\": \"Joses Afe\",\n    \"BirthDay\": \"31\",\n    \"BirthMonth\": \"10\",\n    \"CreatedAt\": \"2026-03-03\",\n    \"NotificationTypesJson\": \"[0,1]\",\n    \"NotifyTimesJson\": \"[2]\",\n    \"UserId\": \"LBrpxQ9Mt5SirJMKpVgo8ZWD2re2\"\n  }\n]");


            subscriptions = subscriptions.Where(x=> x.NotificationTypes.Contains(NotificationType.Email)).ToList();
            var subscriptionsToNotify = subscriptions
                .SelectMany(s => FilterSubcriptions(s)
                    .Select(d => new { Subscription = s, NotifyTime = d.NotifyTime }))
                .ToList();

            if (subscriptionsToNotify.Count == 0)
            {
                Console.WriteLine("No subscription notifications to send today.");
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
                    continue; // skip if no valid email found for the group
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

        private string GetUserEmailForCelebrant(string userId)
        {
            if (userId != null)
            {
                //check if userId exists in _allUsers to avoid potential exceptions
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
            else            {
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
    }
}
