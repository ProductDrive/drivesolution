using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NotificationDomain;
using PD.WhatsAppSender;
using WhatsAppApi.Helpers;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddFileLogging();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
    options.AddPolicy("WhatsAppCorsPolicy", policy =>
    {
        policy.AllowAnyMethod()
        .AllowAnyHeader()
        .AllowAnyOrigin();
    })
);

builder.Services.AddOptions<WhatsAppSettings>()
    .BindConfiguration("WhatsAppSettings")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<EmailApi.Helpers.Secrets>()
    .BindConfiguration("Secretes")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitConnection = builder.Configuration.GetConnectionString("rabbitmq");
        if (string.IsNullOrEmpty(rabbitConnection))
            throw new InvalidOperationException("RabbitMQ connection string not found.");
        cfg.Host(rabbitConnection);
    });
});

var app = builder.Build();

app.UseGlobalExceptionHandler();

app.UseCors("WhatsAppCorsPolicy");

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("notification/whatsapp/send", async (IPublishEndpoint publishEndpoint, IOptions<WhatsAppSettings> defaultSettingsOption, IOptions<EmailApi.Helpers.Secrets> secretsOption, MessageDTO message) =>
{
    if (message == null || message.Contacts == null || !message.Contacts.Any())
    {
        return Results.BadRequest("Message and at least one contact are required");
    }

    var defaultSettings = defaultSettingsOption.Value;
    var secrets = secretsOption.Value;

    message.MessageType = PDMessageType.WhatsApp;
    message.GateWayToUse = defaultSettings.GateWayToUse;

    if (!string.IsNullOrWhiteSpace(message.SenderSettings?.PublicKey))
    {
        var apiKeyService = new WhatsAppApiKeyServices();
        var senderSettings = apiKeyService.RetrieveSenderSettings(secrets, message.SenderSettings.PublicKey);
        if (senderSettings == null)
        {
            return Results.BadRequest("Invalid API key");
        }
        message.SenderSettings = senderSettings;
    }
    else
    {
        message.SenderSettings = new SenderSettingsDTO
        {
            OnBehalf = true,
            Email = defaultSettings.DefaultSenderPhone,
            Domain = "",
            Port = 0
        };
    }

    message.FallBackSenderSettings = message.SenderSettings;

    var validationResult = WhatsAppMessageValidator.Validate(message);
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(validationResult);
    }

    await publishEndpoint.Publish(message);
    return Results.Ok(new { status = "Notification queued", messageType = "WhatsApp" });
});

app.MapPost("notification/whatsapp/send/direct", (IOptions<WhatsAppSettings> defaultSettingsOption, MessageDTO message) =>
{
    if (message == null || message.Contacts == null || !message.Contacts.Any())
    {
        return Results.BadRequest("Message and at least one contact are required");
    }

    message.MessageType = PDMessageType.WhatsApp;
    
    var result = WhatsAppHelper.SendWhatsAppDirect(message);
    
    if (result)
    {
        return Results.Ok(new { status = "WhatsApp sent successfully" });
    }
    else
    {
        return Results.BadRequest("Failed to send WhatsApp message");
    }
});

app.MapPost("notification/whatsapp/getapikey", (IOptions<EmailApi.Helpers.Secrets> secretsOption, SenderSettingsDTO senderSettings) =>
{
    if (string.IsNullOrWhiteSpace(senderSettings.Email))
    {
        return Results.BadRequest("Sender phone number is required");
    }

    var secrets = secretsOption.Value;
    var apiKeyService = new WhatsAppApiKeyServices();
    var publicKey = apiKeyService.GeneratePublicKey(secrets, senderSettings);
    
    return Results.Ok(publicKey);
});

app.MapGet("/health", () => Results.Ok(new { status = "WhatsApp API is running" }));

app.Run();
