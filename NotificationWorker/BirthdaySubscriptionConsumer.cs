using MassTransit;
using NotificationDomain;

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
                Console.WriteLine($"📅 Received birthday subscription for {subscription.Name}");

                // Save subscription to database
                subscription.Id = Guid.NewGuid(); // Ensure a new ID is generated
                _db.BirthdaySubscriptions.Add(subscription);
                await _db.SaveChangesAsync();

                Console.WriteLine($"✅ Subscription saved: ID {subscription.CelebrantId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving subscription: {ex.Message}");
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