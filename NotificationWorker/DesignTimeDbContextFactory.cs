using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NotificationWorker;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        // Temporary connection for design-time (local dev only)
        //var connectionString = "Host=notification-db;Port=5432;Database=notificationdb;Username=postgres;Password=productdrive";
        var connectionString = "Host=localhost;Port=53917;Username=postgres;Password=J*!6A0YCh1aA4GYva-J-fN;Database=notificationdb";

        var optionsBuilder = new DbContextOptionsBuilder<NotificationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new NotificationDbContext(optionsBuilder.Options);
    }
}
