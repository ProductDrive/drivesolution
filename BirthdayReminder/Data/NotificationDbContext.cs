using Microsoft.EntityFrameworkCore;
using NotificationDomain;
using System.Collections.Generic;

namespace BirthdayReminder.Data
{
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
            : base(options) { }

        public DbSet<BirthdaySubscription> BirthdaySubscriptions => Set<BirthdaySubscription>();
    }
}
