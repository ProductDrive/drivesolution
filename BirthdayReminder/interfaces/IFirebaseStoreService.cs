using BirthdayReminder.Models;
using PD.EmailSender.Helpers.Model;

namespace BirthdayReminder.interfaces
{
    public interface IFirebaseStoreService
    {
        Task<List<Celebrant>> GetAllCelebrant();
        Task<ResponseModel> GetUserEmailAsync(string userId);
        Task<Dictionary<string, List<Celebrant>>> CelebrantsByUserIdAsync();
        Task<Dictionary<string, List<Celebrant>>> FetchCelebrantsByUserEmailAsync();
        Task<ResponseModel> CelebrantsByUserEmailAsync();
        List<MessageModel> BuildBirthdayMessages(ResponseModel model, bool today);
        Task<ResponseModel> SendBirthdayEmails(List<MessageModel> messageModels);
    }
}
