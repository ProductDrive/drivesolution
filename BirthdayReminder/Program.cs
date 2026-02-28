using BirthdayReminder.Implementations;
using BirthdayReminder.interfaces;
using BirthdayReminder.Models;
using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using NotificationDomain;
using PD.EmailSender.Helpers.Model;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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

// MassTransit with RabbitMQ - matches EmailApi setup
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitConn = builder.Configuration.GetConnectionString("rabbitmq");
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

app.UseCors("CorsPolicy");
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/reminders", async (IFirebaseStoreService firebaseStore) =>
{
    var celebrants = await firebaseStore.CelebrantsByUserEmailAsync();
    List<MessageModel> messages = new();
    messages.AddRange(firebaseStore.BuildBirthdayMessages(celebrants, true));
    messages.AddRange(firebaseStore.BuildBirthdayMessages(celebrants, false));
    var result = await firebaseStore.SendBirthdayEmails(messages);
    return Results.Ok(result);
});

// New endpoint: POST /api/birthdays/subscribe
app.MapPost("/api/birthdays/subscribe", async (SubscriptionRequest req, IPublishEndpoint publishEndpoint) =>
{
    if (req == null)
        return Results.BadRequest("Invalid payload");

    if (string.IsNullOrWhiteSpace(req.CelebrantId) || string.IsNullOrWhiteSpace(req.Name))
        return Results.BadRequest("CelebrantId and Name are required");

    if (req.BirthDay < 1 || req.BirthDay > 31 || req.BirthMonth < 1 || req.BirthMonth > 12)
        return Results.BadRequest("Invalid BirthDay or BirthMonth");

    if (req.NotificationType == null || req.NotificationType.Count == 0)
        return Results.BadRequest("At least one NotificationType is required");

    // Create domain model
    var subscription = new BirthdaySubscription
    {
        CelebrantId = req.CelebrantId,
        Name = req.Name,
        BirthDay = req.BirthDay,
        BirthMonth = req.BirthMonth,
        NotificationTypes = req.NotificationType,
        NotifyTimes = req.NotifyTimes,
        CreatedAt = DateTime.UtcNow
    };

    // Publish to RabbitMQ for NotificationWorker to consume and save
    await publishEndpoint.Publish(subscription);

    return Results.Accepted("Subscription received and queued for processing");
});

app.MapGet("/testapi", async (IFirebaseStoreService firebaseStore) =>
{
    return Results.Ok("Hit success");
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
