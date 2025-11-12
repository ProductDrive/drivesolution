using MassTransit;
using MassTransit.SqlTransport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationWorker;
using System.Text.RegularExpressions;





var builder = Host.CreateApplicationBuilder(args);

// get connection string (Aspire injects it automatically)
//var connectionString = builder.Configuration.GetConnectionString("notificationdb")?? "Host=notification-db;Port=49979;Username=pddrive;Password=J*!6A0YCh1aA4GYva-J-fN;Database=notificationdb"; //J*!6A0YCh1aA4GYva-J-fN ======= "Host=localhost;Port=49979;Username=postgres;Password=J*!6A0YCh1aA4GYva-J-fN;Database=notificationdb"
var connectionString = builder.Configuration.GetConnectionString("notificationdb");
Console.WriteLine(connectionString);
//string inchange = Regex.Replace(
//    connectionString,
//    @"Password=[^;]*",
//    "Password=productdrive"
//);
//string updatedconnectionString = Regex.Replace(
//    inchange,
//    @"Username=[^;]*",
//    "Username=pddrive"
//);
//Console.WriteLine(connectionString);
//Console.WriteLine(updatedconnectionString);

//builder.Services.AddDbContext<NotificationDbContext>(options =>
//    options.UseNpgsql(updatedconnectionString));
builder.AddNpgsqlDbContext <NotificationDbContext>("notificationdb");


builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<NotificationConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitConn = builder.Configuration.GetConnectionString("rabbitmq");
        cfg.Host(rabbitConn, h =>
        {
            h.Username("rabbitmqUser");
            h.Password("rabbitmqPass");
        });

        cfg.ReceiveEndpoint("notification-queue", e =>
        {
            e.ConfigureConsumer<NotificationConsumer>(context);
        });
    });
});

var host = builder.Build();
host.Run();
