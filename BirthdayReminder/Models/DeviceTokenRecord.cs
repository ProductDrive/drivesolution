using Google.Cloud.Firestore;

namespace BirthdayReminder.Models
{
    [FirestoreData]
    public class DeviceTokenRecord
    {
        [FirestoreProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [FirestoreProperty("token")]
        public string Token { get; set; } = string.Empty;

        [FirestoreProperty("platform")]
        public string Platform { get; set; } = string.Empty;

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }

        [FirestoreProperty("updatedAt")]
        public Timestamp UpdatedAt { get; set; }
    }
}
