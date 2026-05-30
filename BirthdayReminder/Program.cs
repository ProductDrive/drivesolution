using BirthdayReminder.Data;
using BirthdayReminder.Implementations;
using BirthdayReminder.interfaces;
using BirthdayReminder.Models;
using FirebaseAdmin;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddFileLogging();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Cors policy
builder.Services.AddCors(options =>
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowAnyOrigin();
    })
);

builder.Services.AddScoped<IFirebaseStoreService, FirebaseStoreService>();
builder.Services.AddScoped<ISubscriptionNotificationService, SubscriptionNotificationService>();
builder.Services.AddScoped<IDeviceTokenService, DeviceTokenService>();
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();

// Initialize Firebase Admin SDK for FCM push notifications
var firebaseCredPath = $"./Jobstore/{FirebaseBirthdayStore.CredentialsPath}";
Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", firebaseCredPath);
if (FirebaseApp.DefaultInstance == null)
{
    FirebaseApp.Create();
}

// Register local NotificationDbContext pointing to the same Postgres used by the worker
var notificationConn = builder.Configuration.GetConnectionString("notificationdb");
if (!string.IsNullOrWhiteSpace(notificationConn))
{
    //builder.Services.AddDbContext<NotificationDbContext>(options => options.UseNpgsql(notificationConn));
    builder.AddNpgsqlDbContext<NotificationDbContext>("notificationdb");
}

// MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        //var rabbitConnection = builder.Configuration.GetConnectionString("rabbitmq");
        //if (string.IsNullOrEmpty(rabbitConnection))
        //    throw new InvalidOperationException("RabbitMQ connection string not found.");
        //cfg.Host(rabbitConnection);

        //=================================PROD===========================================
        //var rabbitConn = builder.Configuration.GetConnectionString("rabbitmq");
        var rabuser = builder.Configuration["RABBITMQUSER"];
        var rabpas = builder.Configuration["RABBITMQPASS"];

        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username(rabuser);
            h.Password(rabpas);
        });
    });
});

var app = builder.Build();

app.UseGlobalExceptionHandler();

app.UseCors("CorsPolicy");
app.MapDefaultEndpoints();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/testapi", () =>
{
    return Results.Ok("Hit success");
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
