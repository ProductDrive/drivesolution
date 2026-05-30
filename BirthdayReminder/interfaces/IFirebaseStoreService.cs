using BirthdayReminder.Models;

namespace BirthdayReminder.interfaces
{
    public interface IFirebaseStoreService
    {
        Task<List<Celebrant>> GetAllCelebrant();
        Task<ResponseModel> GetUserEmailAsync(string userId);
        Task<Dictionary<string, List<Celebrant>>> CelebrantsByUserIdAsync();
        Task<Dictionary<string, List<Celebrant>>> FetchCelebrantsByUserEmailAsync();
        Task<ResponseModel> CelebrantsByUserEmailAsync();
        List<PD.EmailSender.Helpers.Model.MessageModel> BuildBirthdayMessages(ResponseModel model, bool today);
        Task<ResponseModel> SendBirthdayEmails(List<PD.EmailSender.Helpers.Model.MessageModel> messageModels);
        Task<List<UserRecord>> GetAllUsers();
        Task<ResponseModel> CelebrantsByWhatsAppAsync();
        List<PD.EmailSender.Helpers.Model.MessageModel> BuildWhatsAppBirthdayMessages(ResponseModel model, bool today);
        Task<ResponseModel> SendBirthdayWhatsApp(List<PD.EmailSender.Helpers.Model.MessageModel> messageModels);
    }
}
