using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("env");

// create parameters (secret true -> treated as secret; values come from AppHost Parameters config)
var rabbitUser = builder.AddParameter("rabbitmqUser", secret: true);
var rabbitPass = builder.AddParameter("rabbitmqPass", secret: true);

// 🗄️ PostgreSQL setup
var postgres = builder.AddPostgres("notification-db")
    .WithDataVolume() // persist data
    .WithPgAdmin();

var notificationDb = postgres.AddDatabase("notificationdb");


// add rabbitmq server resource and pass the parameters for username & password
var rabbit = builder.AddRabbitMQ("rabbitmq", rabbitUser, rabbitPass)
                    .WithManagementPlugin()   // optional: enable management UI
                    .WithDataVolume();        // optional: persist data across runs

builder.AddProject<Projects.EmailApi>("api")
       .WithReference(rabbit)
       .WaitFor(rabbit);

builder.AddProject<Projects.NotificationWorker>("consumer")
        .WithReference(notificationDb)
       .WithReference(rabbit)
       .WaitFor(rabbit);

// other resources...
var cache = builder.AddRedis("cache");
var apiService = builder.AddProject<Projects.DriveSolution_ApiService>("apiservice")
                        .WithReference(cache);

builder.AddProject<Projects.BirthdayReminder>("birthdayreminder");

builder.Build().Run();
