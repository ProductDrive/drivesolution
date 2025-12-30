using Google.Cloud.Firestore;

namespace BirthdayReminder.Models
{
    [FirestoreData]
    public class UserRecord
    {
        [FirestoreProperty("userId")]
        public string UserId { get; set; }

        [FirestoreProperty("email")]
        public string Email { get; set; }
    }
}
