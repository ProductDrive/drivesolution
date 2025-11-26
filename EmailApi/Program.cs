using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NotificationDomain;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
// 1️⃣ Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOptions<SenderSettingsDTO>()
    .BindConfiguration("EmailSuperSender")
    .ValidateDataAnnotations()
    .ValidateOnStart();

// MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitConn = builder.Configuration.GetConnectionString("rabbitmq");
        cfg.Host(rabbitConn, h =>
        {
            h.Username("rabbitmqUser");
            h.Password("rabbitmqPass");
        });
    });
});

var app = builder.Build();

// 3️⃣ Enable Swagger middleware

app.UseSwagger();
app.UseSwaggerUI();

// Minimal API endpoint to publish message
app.MapPost("notification/email/send/onbehalf", async (IPublishEndpoint publishEndpoint, IOptions<SenderSettingsDTO> fallBackSenderOption, MessageDTO emailMsg) =>
{
    SenderSettingsDTO fallBackSender = fallBackSenderOption.Value;
    Console.WriteLine("environment", fallBackSender);
    emailMsg.FallBackSenderSettings = fallBackSender;
    emailMsg.SenderSettings = fallBackSender;
    var isMessageObjectValid = MessageDTOValidator.Validate(emailMsg);
    if (isMessageObjectValid.IsValid)
    {
        emailMsg.SenderSettings.OnBehalf = true;

        await publishEndpoint.Publish(emailMsg);
        return Results.Ok("Notification sent!");
    }
    else
    {
        return Results.BadRequest(isMessageObjectValid);
    }


});

// Minimal API to return a string "site updated"
app.MapGet("/updates", () =>
{
    return Results.Ok("Lets see if watch tower works");
});

app.Run();
