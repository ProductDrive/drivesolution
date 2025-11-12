using Microsoft.EntityFrameworkCore;
using NotificationDomain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationWorker
{
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
            : base(options) { }

        public DbSet<NotificationRecord> Notifications => Set<NotificationRecord>();
    }
}
