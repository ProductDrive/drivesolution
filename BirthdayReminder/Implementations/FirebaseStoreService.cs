using BirthdayReminder.interfaces;
using BirthdayReminder.Models;
using Google.Cloud.Firestore;
using Google.Protobuf;
using NotificationDomain;
using PD.EmailSender.Helpers;
using PD.EmailSender.Helpers.Model;
using PD.WhatsAppSender;
using System.Text;
using WhatsAppContactsModel = NotificationDomain.ContactsModel;
using static System.Net.Mime.MediaTypeNames;

namespace BirthdayReminder.Implementations
{
    public class FirebaseStoreService : IFirebaseStoreService
    {
        private readonly FirestoreDb firestoreDb;
        public FirebaseStoreService()
        {

            var credentialsPath = $"./Jobstore/{FirebaseBirthdayStore.CredentialsPath}";
            //var credentialsPath = $"./Jobstore/{FirebaseBirthdayStore.CredentialsPath}";

            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
            firestoreDb = FirestoreDb.Create(FirebaseBirthdayStore.ProjectId);


        }

        public async Task<List<Celebrant>> GetAllCelebrant()
        {
            var collectionRef = firestoreDb.Collection("celebrants");
            var snapshot = await collectionRef.GetSnapshotAsync();

            return snapshot.Documents
                                     .Select(doc => doc.ConvertTo<Celebrant>())
                                     .ToList();

        }

        public async Task<List<UserRecord>> GetAllUsers()
        {
            var collectionRef = firestoreDb.Collection("users");
            var snapshot = await collectionRef.GetSnapshotAsync();

            return snapshot.Documents
                                     .Select(doc => doc.ConvertTo<UserRecord>())
                                     .ToList();

        }

        //TODO: optimize this query
        public async Task<ResponseModel> GetUserEmailAsync(string userId)
        {
            var query = firestoreDb.Collection("users").WhereEqualTo("userId", userId);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count != 0)
            {
                var doc = snapshot.Documents[0];
                var userRec = doc.ConvertTo<UserRecord>();
                return new ResponseModel { Response = "successful", ReturnObj = new { Email = userRec.Email }, Status = true };
            }

            return new ResponseModel { Response = "failed", Status = false };
        }

        //Group celebrants under users using userId
        public async Task<Dictionary<string, List<Celebrant>>> CelebrantsByUserIdAsync()
        {
            var celebrants = await GetAllCelebrant();
            // filter or select celebrants whose birthday is today or tomorrow
            celebrants = celebrants.Where(c =>
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                var birthDateThisYear = new DateTime(today.Year, c.BirthMonth, c.BirthDay);
                return birthDateThisYear == today || birthDateThisYear == tomorrow;
            }).ToList();

            var groupedCelebrants = celebrants
                .GroupBy(c => c.UserId)
                .ToDictionary(g => g.Key, g => g.ToList());
            return groupedCelebrants;
        }

        // from the celebrants group, get the user email for each userId
        public async Task<ResponseModel> CelebrantsByUserEmailAsync()
        {
            var celebrantsByUserId = await CelebrantsByUserIdAsync();
            var result = new Dictionary<string, List<Celebrant>>();

            foreach (var entry in celebrantsByUserId)
            {
                var userId = entry.Key;
                var celebrants = entry.Value;

                var response = await GetUserEmailAsync(userId);
                if (response.Status)
                {
                    var email = ((dynamic)response.ReturnObj).Email;
                    result[email] = celebrants;
                }
            }

            return new ResponseModel { Response = "success", ReturnObj = result, Status = true };
        }

        // Build Birthday email message for each user email using MessageDTO
        public List<PD.EmailSender.Helpers.Model.MessageModel> BuildBirthdayMessages(ResponseModel model, bool today)
        {

            //convert return object to dictionary   
            var celebrantsByEmail = (Dictionary<string, List<Celebrant>>)model.ReturnObj;
            List<MessageModel> messages = new();
            var displayName = new List<string>();

            foreach (var entry in celebrantsByEmail)
            {
                var messageDTO = new MessageModel();
                var email = entry.Key;
                var celebrants = entry.Value;
                if (today)
                {
                    //filter celebrants whose birthday is today
                    var todayDate = DateTime.Today;
                    celebrants = celebrants.Where(c => c.BirthDate.Month == todayDate.Month && c.BirthDate.Day == todayDate.Day).ToList();

                }
                else
                {
                    //filter celebrants whose birthday is tomorrow
                    var tomorrowDate = DateTime.Today.AddDays(1);
                    celebrants = celebrants.Where(c => c.BirthDate.Month == tomorrowDate.Month && c.BirthDate.Day == tomorrowDate.Day).ToList();
                }
                if (celebrants.Count == 0)
                {
                    continue; // skip if no celebrants for today or tomorrow
                }
                displayName.AddRange(celebrants.Select(x => x.Name));
                var plural = celebrants.Count > 1 ? "s" : "";
                var subject = today ? $"Reminder: {celebrants.Count} Birthday{plural} TODAY!" : $"Reminder: {celebrants.Count} Birthday{plural} Coming Up Tomorrow";

                var sb = new StringBuilder();
                sb.AppendLine($"<h2>Upcoming Birthdays</h2>");
                sb.AppendLine("<ul>");
                foreach (var celebrant in celebrants)
                {
                    sb.AppendLine($"<li><strong>{celebrant.Name}</strong> - {celebrant.BirthDate:MMMM dd}");

                    sb.AppendLine("</li>");
                }
                sb.AppendLine("</ul>");
                sb.AppendLine("Go to birthday reminder app to send your wishes.");
                // append birthday app link
                sb.AppendLine("<p><a href='https://drive-birthday-reminder.vercel.app/auth'>Birthday Reminder App</a></p>");

                messageDTO.Contacts  = new List<PD.EmailSender.Helpers.Model.ContactsModel> { new PD.EmailSender.Helpers.Model.ContactsModel { Email = email } };
                messageDTO.Subject = subject;
                messageDTO.Message = sb.ToString();
                if (displayName.Count > 1)
                {
                    messageDTO.EmailDisplayName = $"{String.Join(',', displayName.Take(2))}... Birthdays";
                }
                if (displayName.Count == 1)
                {
                    messageDTO.EmailDisplayName = $"{displayName[0]}'s Birthday";
                }
                messages.Add(messageDTO);
            }
            
            return messages;

        }

        public async Task<ResponseModel> SendBirthdayEmails(List<PD.EmailSender.Helpers.Model.MessageModel> messageModels)
        {
            if (messageModels.Count == 0)
            {
                return new ResponseModel { Response = "No birthday today", Status = true };
            }
            try
            {
                foreach (var item in messageModels)
                {
                    item.SenderSettings.OnBehalf = true;
                    item.EmailDisplayName =   item.EmailDisplayName;
                    await SendMailVTwo.SendSingleEmailOnBehalf(item);
                }
                return new ResponseModel { Status = true, Response = "Email sent" };
            }
            catch (Exception ex)
            {
                return new ResponseModel { Response = ex.Message, Status = false };
            }
        }


        public Task<Dictionary<string, List<Celebrant>>> FetchCelebrantsByUserEmailAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<ResponseModel> CelebrantsByWhatsAppAsync()
        {
            var celebrantsByUserId = await CelebrantsByUserIdAsync();
            var result = new Dictionary<string, List<Celebrant>>();
            var allUsers = await GetAllUsers();

            foreach (var entry in celebrantsByUserId)
            {
                var userId = entry.Key;
                var celebrants = entry.Value;

                var user = allUsers.FirstOrDefault(u => u.UserId == userId);
                if (user != null && !string.IsNullOrWhiteSpace(user.WhatsappNumber))
                {
                    result[user.WhatsappNumber] = celebrants;
                }
            }

            return new ResponseModel { Response = "success", ReturnObj = result, Status = true };
        }

        public List<PD.EmailSender.Helpers.Model.MessageModel> BuildWhatsAppBirthdayMessages(ResponseModel model, bool today)
        {
            var celebrantsByPhone = (Dictionary<string, List<Celebrant>>)model.ReturnObj;
            List<PD.EmailSender.Helpers.Model.MessageModel> messages = new();
            var displayName = new List<string>();

            foreach (var entry in celebrantsByPhone)
            {
                var phoneNumber = entry.Key;
                var celebrants = entry.Value;

                if (today)
                {
                    var todayDate = DateTime.Today;
                    celebrants = celebrants.Where(c => c.BirthDate.Month == todayDate.Month && c.BirthDate.Day == todayDate.Day).ToList();
                }
                else
                {
                    var tomorrowDate = DateTime.Today.AddDays(1);
                    celebrants = celebrants.Where(c => c.BirthDate.Month == tomorrowDate.Month && c.BirthDate.Day == tomorrowDate.Day).ToList();
                }

                if (celebrants.Count == 0)
                {
                    continue;
                }

                displayName.AddRange(celebrants.Select(x => x.Name));
                var plural = celebrants.Count > 1 ? "s" : "";
                var subject = today ? $"Reminder: {celebrants.Count} Birthday{plural} TODAY!" : $"Reminder: {celebrants.Count} Birthday{plural} Coming Up Tomorrow";

                var sb = new StringBuilder();
                sb.AppendLine($"🎂 *Birthday Reminder*");
                sb.AppendLine();
                foreach (var celebrant in celebrants)
                {
                    sb.AppendLine($"• *{celebrant.Name}* - {celebrant.BirthDate:MMMM dd}");
                }
                sb.AppendLine();
                sb.AppendLine("Go to birthday reminder app to send your wishes.");

                var messageDTO = new PD.EmailSender.Helpers.Model.MessageModel();
                messageDTO.Contacts = new List<PD.EmailSender.Helpers.Model.ContactsModel> { new PD.EmailSender.Helpers.Model.ContactsModel { Phone = phoneNumber } };
                messageDTO.Subject = subject;
                messageDTO.Message = sb.ToString();
                messageDTO.MessageType = PD.EmailSender.Helpers.Model.PDMessageType.WhatsApp;

                if (displayName.Count > 1)
                {
                    messageDTO.EmailDisplayName = $"{String.Join(',', displayName.Take(2))}... Birthdays";
                }
                if (displayName.Count == 1)
                {
                    messageDTO.EmailDisplayName = $"{displayName[0]}'s Birthday";
                }
                messages.Add(messageDTO);
            }

            return messages;
        }

        public async Task<ResponseModel> SendBirthdayWhatsApp(List<PD.EmailSender.Helpers.Model.MessageModel> messageModels)
        {
            if (messageModels.Count == 0)
            {
                return new ResponseModel { Response = "No birthday today", Status = true };
            }
            try
            {
                foreach (var item in messageModels)
                {
                    var whatsappContacts = item.Contacts.Select(c => new NotificationDomain.ContactsModel { Phone = c.Phone ?? "", Email = c.Email }).ToList();
                    var result = WhatsAppHelper.SendWhatsAppDirect(new NotificationDomain.MessageDTO
                    {
                        MessageType = NotificationDomain.PDMessageType.WhatsApp,
                        Subject = item.Subject,
                        Message = item.Message,
                        Contacts = whatsappContacts,
                        SenderSettings = new NotificationDomain.SenderSettingsDTO { OnBehalf = true },
                        FallBackSenderSettings = new NotificationDomain.SenderSettingsDTO { OnBehalf = true }
                    });
                }
                return new ResponseModel { Status = true, Response = "WhatsApp sent" };
            }
            catch (Exception ex)
            {
                return new ResponseModel { Response = ex.Message, Status = false };
            }
        }
    }
}
