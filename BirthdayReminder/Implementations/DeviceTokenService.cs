using BirthdayReminder.interfaces;
using BirthdayReminder.Models;
using Google.Cloud.Firestore;

namespace BirthdayReminder.Implementations
{
    public class DeviceTokenService : IDeviceTokenService
    {
        private readonly FirestoreDb _firestoreDb;

        public DeviceTokenService()
        {
            var credentialsPath = $"./Jobstore/{FirebaseBirthdayStore.CredentialsPath}";
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
            _firestoreDb = FirestoreDb.Create(FirebaseBirthdayStore.ProjectId);
        }

        private CollectionReference TokenCollection => _firestoreDb.Collection("device_tokens");

        public async Task RegisterTokenAsync(string userId, string token, string platform)
        {
            var existing = await TokenCollection
                .WhereEqualTo("token", token)
                .GetSnapshotAsync();

            var now = Timestamp.GetCurrentTimestamp();

            if (existing.Documents.Count > 0)
            {
                var doc = existing.Documents[0];
                await doc.Reference.UpdateAsync(new Dictionary<string, object>
                {
                    { "userId", userId },
                    { "platform", platform },
                    { "updatedAt", now }
                });
            }
            else
            {
                await TokenCollection.AddAsync(new DeviceTokenRecord
                {
                    UserId = userId,
                    Token = token,
                    Platform = platform,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        public async Task UnregisterTokenAsync(string userId, string token)
        {
            var existing = await TokenCollection
                .WhereEqualTo("token", token)
                .WhereEqualTo("userId", userId)
                .GetSnapshotAsync();

            foreach (var doc in existing.Documents)
            {
                await doc.Reference.DeleteAsync();
            }
        }

        public async Task<List<string>> GetUserTokensAsync(string userId)
        {
            var snapshot = await TokenCollection
                .WhereEqualTo("userId", userId)
                .GetSnapshotAsync();

            return snapshot.Documents
                .Select(d => d.ConvertTo<DeviceTokenRecord>())
                .Where(r => !string.IsNullOrWhiteSpace(r.Token))
                .Select(r => r.Token)
                .ToList();
        }

        public async Task<List<string>> GetAllTokensAsync()
        {
            var snapshot = await TokenCollection.GetSnapshotAsync();

            return snapshot.Documents
                .Select(d => d.ConvertTo<DeviceTokenRecord>())
                .Where(r => !string.IsNullOrWhiteSpace(r.Token))
                .Select(r => r.Token)
                .ToList();
        }

        public async Task RemoveTokenAsync(string token)
        {
            var existing = await TokenCollection
                .WhereEqualTo("token", token)
                .GetSnapshotAsync();

            foreach (var doc in existing.Documents)
            {
                await doc.Reference.DeleteAsync();
            }
        }
    }
}
