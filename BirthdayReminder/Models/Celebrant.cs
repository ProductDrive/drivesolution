using Google.Cloud.Firestore;

namespace BirthdayReminder.Models
{
    [FirestoreData]
    public class Celebrant
    {
        [FirestoreDocumentId] // Maps the Firestore document ID (optional)
        public string Id { get; set; }

        [FirestoreProperty("name")]
        public string Name { get; set; }

        [FirestoreProperty("birthDay")]
        public int BirthDay { get; set; }

        [FirestoreProperty("birthMonth")]
        public int BirthMonth { get; set; }

        [FirestoreProperty("pictureUrl")]
        public string PictureUrl { get; set; }

        [FirestoreProperty("message")]
        public string Message { get; set; }

        [FirestoreProperty("userId")]
        public string UserId { get; set; }

        public DateTime BirthDate => new DateTime(DateTime.Now.Year, BirthMonth, BirthDay);
    }
}
