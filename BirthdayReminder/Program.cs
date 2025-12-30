using BirthdayReminder.Implementations;
using BirthdayReminder.interfaces;
using PD.EmailSender.Helpers.Model;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IFirebaseStoreService, FirebaseStoreService>();


var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/reminders", async (IFirebaseStoreService firebaseStore) =>
{
    var celebrants = await firebaseStore.CelebrantsByUserEmailAsync();
    List<MessageModel> messages = new();
    messages.AddRange(firebaseStore.BuildBirthdayMessages(celebrants, true));
    messages.AddRange(firebaseStore.BuildBirthdayMessages(celebrants, false));
    var result = await firebaseStore.SendBirthdayEmails(messages);
    return Results.Ok(result);
});


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
