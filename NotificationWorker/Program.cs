using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NotificationDomain;
using NotificationWorker;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("notificationdb");
Console.WriteLine(connectionString);

// register EF using your existing Aspire helper
builder.AddNpgsqlDbContext<NotificationDbContext>("notificationdb");

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<NotificationConsumer>(cfg =>
    {
        // per-consumer options can go here
    });
    x.AddConsumer<BirthdaySubscriptionConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitConn = builder.Configuration.GetConnectionString("rabbitmq");
        var rabuser = builder.Configuration["RABBITMQUSER"];
        var rabpas = builder.Configuration["RABBITMQPASS"];

        if (!string.IsNullOrWhiteSpace(rabbitConn))
        {
            // if you provide a full amqp URI in configuration
            cfg.Host(new Uri(rabbitConn), h =>
            {
                if (!string.IsNullOrWhiteSpace(rabuser)) h.Username(rabuser);
                if (!string.IsNullOrWhiteSpace(rabpas)) h.Password(rabpas);
            });
        }
        else
        {
            cfg.Host("rabbitmq", "/", h =>
            {
                h.Username(rabuser);
                h.Password(rabpas);
            });
        }

        // Separate endpoints for clearer routing and scaling
        cfg.ReceiveEndpoint("notification-queue", e =>
        {
            // tune concurrency/prefetch
            e.PrefetchCount = 16;
            e.ConcurrentMessageLimit = 8;

            // simple retry policy
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));

            e.ConfigureConsumer<NotificationConsumer>(context);
        });

        cfg.ReceiveEndpoint("birthday-subscription-queue", e =>
        {
            e.PrefetchCount = 8;
            e.ConcurrentMessageLimit = 4;
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));

            e.ConfigureConsumer<BirthdaySubscriptionConsumer>(context);
        });

        // optionally: configure global topology or durable queue settings here
    });
});

var host = builder.Build();
host.Run();
