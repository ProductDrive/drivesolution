namespace BirthdayReminder.interfaces
{
    public interface IPushNotificationService
    {
        Task SendToUserAsync(string userId, string title, string body);
        Task SendToTokenAsync(string token, string title, string body);
        Task SendToMultipleUsersAsync(List<string> userIds, string title, string body);
        Task SendToAllAsync(string title, string body);
    }
}
