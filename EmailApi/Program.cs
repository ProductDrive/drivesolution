using EmailApi.Helpers;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NotificationDomain;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
// 1️⃣ Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOptions<SenderSettingsDTO>()
    .BindConfiguration("EmailSuperSender")
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<Secrets>()
    .BindConfiguration("Secretes")
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


app.MapPost("notification/email/send", async (IPublishEndpoint publishEndpoint, IOptions<SenderSettingsDTO> fallBackSenderOption, IOptions <Secrets> SecretesOption, MessageDTO emailMsg) =>
{
    if (string.IsNullOrWhiteSpace(emailMsg?.SenderSettings?.PublicKey))
    {
        return Results.BadRequest("Public Key is required to use this service. If you want a third party sender, use the 'notification/email/send/onbehalf' endpoint");
    }

    APIKeyServices aPIKeyServices = new APIKeyServices();
    emailMsg.FallBackSenderSettings = fallBackSenderOption.Value;
    var decodedSender = aPIKeyServices.RetrieveSenderSettings(SecretesOption.Value, emailMsg.SenderSettings.PublicKey);
    if (!SenderSettingsValidator.Validate(decodedSender).IsValid)
    {
        return Results.BadRequest("Invalid Public Key");
    }
    emailMsg.SenderSettings = decodedSender;
    var isMessageObjectValid = MessageDTOValidator.Validate(emailMsg);
    if (isMessageObjectValid.IsValid)
    {
        emailMsg.SenderSettings.OnBehalf = false;
        
        
        await publishEndpoint.Publish(emailMsg);
        return Results.Ok("Notification sent!");
    }
    else
    {
        return Results.BadRequest(isMessageObjectValid);
    }
});

app.MapPost("notification/email/getapikey", (IOptions<Secrets> SecretesOption, SenderSettingsDTO senderSettings) =>
{
    var isSenderObjectValid = SenderSettingsValidator.Validate(senderSettings);
    if (isSenderObjectValid.IsValid)
    {
        APIKeyServices aPIKeyServices = new APIKeyServices();
        var publickey = aPIKeyServices.GeneratePublicKey(SecretesOption.Value, senderSettings);
        return Results.Ok(publickey);
    }
    else
    {
        return Results.BadRequest(isSenderObjectValid);
    }
});

// Minimal API to return a string "site updated"
app.MapGet("/updates", () =>
{
    return Results.Ok("Lets see if watch tower works");
});

app.Run();
