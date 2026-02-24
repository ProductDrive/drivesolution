using BirthdayReminder.Implementations;
using BirthdayReminder.interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using PD.EmailSender.Helpers.Model;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


//Add Cors policy
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


var app = builder.Build();
app.UseCors("CorsPolicy");
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.

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

app.MapGet("/testapi", async (IFirebaseStoreService firebaseStore) =>
{
    return Results.Ok("Hit success");
    //var celebrants = await firebaseStore.CelebrantsByUserEmailAsync();
    //List<MessageModel> messages = new();
    //messages.Add(
    //    new MessageModel
    //    {
    //        Contacts = new List<ContactsModel> { new ContactsModel { Email = "afeexclusive@gmail.com" } },
    //         EmailDisplayName = "PD Birthdays",
    //          Subject = "Birthday reminder from PD"
    //    }

    //);

    //var result = await firebaseStore.SendBirthdayEmails(messages);
    //return Results.Ok(result);
});


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
