using BirthdayReminder.interfaces;
using FirebaseAdmin.Messaging;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

namespace BirthdayReminder.Implementations
{
    public class PushNotificationService : IPushNotificationService
    {
        private readonly IDeviceTokenService _deviceTokenService;

        public PushNotificationService(IDeviceTokenService deviceTokenService)
        {
            _deviceTokenService = deviceTokenService;
        }

        public async Task SendToUserAsync(string userId, string title, string body)
        {
            var tokens = await _deviceTokenService.GetUserTokensAsync(userId);
            await SendToTokensAsync(tokens, title, body);
        }

        public async Task SendToTokenAsync(string token, string title, string body)
        {
            await SendToTokensAsync(new List<string> { token }, title, body);
        }

        public async Task SendToMultipleUsersAsync(List<string> userIds, string title, string body)
        {
            var tasks = userIds.Select(uid => GetUserTokensAndSendAsync(uid, title, body));
            await Task.WhenAll(tasks);
        }

        public async Task SendToAllAsync(string title, string body)
        {
            var tokens = await _deviceTokenService.GetAllTokensAsync();
            await SendToTokensAsync(tokens, title, body);
        }

        private async Task GetUserTokensAndSendAsync(string userId, string title, string body)
        {
            var tokens = await _deviceTokenService.GetUserTokensAsync(userId);
            await SendToTokensAsync(tokens, title, body);
        }

        private async Task SendToTokensAsync(List<string> tokens, string title, string body)
        {
            if (tokens.Count == 0) return;

            var messaging = FirebaseMessaging.DefaultInstance;

            foreach (var token in tokens)
            {
                try
                {
                    var message = new Message
                    {
                        Token = token,
                        Notification = new Notification
                        {
                            Title = title,
                            Body = body
                        }
                    };

                    await messaging.SendAsync(message);
                }
                catch (FirebaseMessagingException ex)
                {
                    if (ex.Message.Contains("Unregistered") || ex.Message.Contains("NOT_FOUND") || ex.HttpResponse?.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        await _deviceTokenService.RemoveTokenAsync(token);
                    }
                    Console.WriteLine($"Failed to send push to token {token}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send push to token {token}: {ex.Message}");
                }
            }
        }
    }
}
