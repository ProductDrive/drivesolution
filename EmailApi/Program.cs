using MassTransit;
using NotificationDomain;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
// 1️⃣ Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
app.MapPost("/send", async (IPublishEndpoint publishEndpoint, string recipient, string subject) =>
{
    await publishEndpoint.Publish(new MessageDTO
    {
        ToContacts = string.IsNullOrWhiteSpace(recipient)? "afeexclusive@gmail.com":recipient,
        Subject = string.IsNullOrWhiteSpace(subject) ? "Testing email":subject,
        Message = "Hello Testing Aspire"
    });
    return Results.Ok("Notification sent!");
});

// Minimal API to return a string "site updated"
app.MapGet("/updates", () =>
{
    return Results.Ok("Lets see if watch tower works");
});

app.Run();
