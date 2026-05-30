# RabbitMQ Connection Issue Analysis (.NET Aspire Project)

## Root Causes
### 1. Invalid RabbitMQ Host Configuration in EmailApi
`EmailApi/Program.cs:58-62` configures MassTransit to use `"/"` as the RabbitMQ host, which is invalid. The host is hardcoded to `"/"` instead of using the Aspire-managed connection string or service name.

### 2. Services Do Not Use Aspire-Injected Connection Strings
All services (EmailApi, WhatsAppApi, BirthdayReminder, NotificationWorker) manually configure RabbitMQ host/username/password instead of using the `rabbitmq` connection string automatically injected by Aspire when `WithReference(rabbit)` is applied. Aspire provides a connection string in the format `amqp://user:pass@host:port/vhost` that includes all necessary connection details.

### 3. Missing RabbitMQ Reference for BirthdayReminder
`DriveSolution.AppHost/AppHost.cs:38` adds the BirthdayReminder project without `WithReference(rabbit)`, so it receives no RabbitMQ connection configuration at all.

### 4. Unreliable Credential Handling
WhatsAppApi, BirthdayReminder, and NotificationWorker read `RABBITMQUSER`/`RABBITMQPASS` from configuration, but Aspire does not set these environment variables. The credentials are embedded in the injected `rabbitmq` connection string, so relying on unset env vars leads to failed authentication.

## Solutions
### 1. Update All Services to Use Aspire-Injected Connection String
For each service, modify the MassTransit configuration to use `builder.Configuration.GetConnectionString("rabbitmq")`:
```csharp
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
```
For consumer services (NotificationWorker), retain existing receive endpoint configurations.

### 2. Fix AppHost to Reference RabbitMQ for BirthdayReminder
Update `DriveSolution.AppHost/AppHost.cs:38` to:
```csharp
builder.AddProject<Projects.BirthdayReminder>("birthdayreminder")
       .WithReference(rabbit)
       .WaitFor(rabbit);
```

### 3. Remove Hardcoded Credentials and Unused Code
- Delete hardcoded username/password in EmailApi (`h.Username("rabbitmqUser"); h.Password("rabbitmqPass");`)
- Remove unused `RABBITMQUSER`/`RABBITMQPASS` reads from all services
- Clean up commented configuration blocks in all service Program.cs files

### 4. Verify Aspire Parameter Configuration
Ensure `DriveSolution.AppHost/appsettings.json` has valid default values for `Parameters:rabbitmqUser` and `Parameters:rabbitmqPass` (currently set to `rabbitmqUser`/`rabbitmqPass`). For production, use user secrets or secure environment variables for these secret parameters.
