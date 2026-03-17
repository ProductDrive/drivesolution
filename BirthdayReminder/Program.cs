using BirthdayReminder.Data;
using BirthdayReminder.Implementations;
using BirthdayReminder.interfaces;
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

// Register local NotificationDbContext pointing to the same Postgres used by the worker
var notificationConn = builder.Configuration.GetConnectionString("notificationdb");
if (!string.IsNullOrWhiteSpace(notificationConn))
{
    //builder.Services.AddDbContext<NotificationDbContext>(options => options.UseNpgsql(notificationConn));
    builder.AddNpgsqlDbContext<NotificationDbContext>("notificationdb");
}

// MassTransit with RabbitMQ - matches EmailApi setup
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
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
