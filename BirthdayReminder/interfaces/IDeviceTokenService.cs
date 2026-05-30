namespace BirthdayReminder.interfaces
{
    public interface IDeviceTokenService
    {
        Task RegisterTokenAsync(string userId, string token, string platform);
        Task UnregisterTokenAsync(string userId, string token);
        Task<List<string>> GetUserTokensAsync(string userId);
        Task<List<string>> GetAllTokensAsync();
        Task RemoveTokenAsync(string token);
    }
}
