using MassTransit;
using Microsoft.EntityFrameworkCore;
using NotificationDomain;
using System.Text.Json;

namespace NotificationWorker
{
    public class BirthdaySubscriptionConsumer : IConsumer<BirthdaySubscription>
    {
        private readonly NotificationDbContext _db;

        public BirthdaySubscriptionConsumer(NotificationDbContext db)
        {
            _db = db;
        }

        public async Task Consume(ConsumeContext<BirthdaySubscription> context)
        {
            try
            {
                var subscription = context.Message;
                Console.WriteLine($"📅 Received birthday subscription for {subscription.Name} (CelebrantId: {subscription.CelebrantId})");

                // Check if subscription exists by CelebrantId
                var existingSubscription = await _db.BirthdaySubscriptions
                    .FirstOrDefaultAsync(s => s.CelebrantId == subscription.CelebrantId);

                if (existingSubscription != null)
                {
                    // Update existing subscription
                    Console.WriteLine($"📝 Updating existing subscription for {existingSubscription.Name}");
                    
                    existingSubscription.Name = subscription.Name;
                    existingSubscription.BirthDay = subscription.BirthDay;
                    existingSubscription.BirthMonth = subscription.BirthMonth;
                    existingSubscription.NotificationTypesJson = subscription.NotificationTypesJson;
                    existingSubscription.NotifyTimesJson = subscription.NotifyTimesJson;
                    existingSubscription.UserId = subscription.UserId;
                    existingSubscription.CreatedAt = subscription.CreatedAt;

                    _db.BirthdaySubscriptions.Update(existingSubscription);
                    await _db.SaveChangesAsync();

                    Console.WriteLine($"✅ Subscription updated: ID {existingSubscription.Id}, CelebrantId {subscription.CelebrantId}");
                }
                else
                {
                    // Create new subscription
                    Console.WriteLine($"➕ Creating new subscription for {subscription.Name}");
                    if (subscription.NotifyTimes?.Count != 0)
                    {
                        subscription.NotifyTimesJson = JsonSerializer.Serialize(subscription.NotifyTimes);
                    }
                    if (subscription.NotificationTypes?.Count != 0)
                    {
                        subscription.NotificationTypesJson = JsonSerializer.Serialize(subscription.NotificationTypes);
                    }



                    _db.BirthdaySubscriptions.Add(subscription);
                    await _db.SaveChangesAsync();

                    Console.WriteLine($"✅ Subscription created: ID {subscription.Id}, CelebrantId {subscription.CelebrantId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing subscription: {ex.Message}");
                // Optionally save error record
                var errorRecord = new NotificationRecord
                {
                    NotificationType = "BirthdaySubscription",
                    Exception = ex.InnerException?.Message ?? ex.Message,
                    Recipient = context.Message.CelebrantId,
                    Subject = $"Subscription for {context.Message.Name}",
                    SourceApp = "BirthdayReminder",
                    SentAt = DateTime.UtcNow,

                };
                _db.Notifications.Add(errorRecord);
                await _db.SaveChangesAsync();
            }
        }
    }
}