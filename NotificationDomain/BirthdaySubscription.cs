using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace NotificationDomain
{
    public class BirthdaySubscription
    {
        public Guid Id { get; set; }
        public string CelebrantId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int BirthDay { get; set; }
        public int BirthMonth { get; set; }
        
        // Stored as JSON: ["whatsapp","email"]
        public string NotificationTypesJson { get; set; } = "[]";
        
        // Stored as JSON: ["1month","2weeks","3days"]
        public string NotifyTimesJson { get; set; } = "[]";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Convenience accessor
        [NotMapped]
        public List<string> NotificationTypes
        {
            get => string.IsNullOrWhiteSpace(NotificationTypesJson) 
                ? new List<string>() 
                : JsonSerializer.Deserialize<List<string>>(NotificationTypesJson) ?? new List<string>();
            set => NotificationTypesJson = JsonSerializer.Serialize(value ?? new List<string>());
        }
        [NotMapped]
        public List<string> NotifyTimes
        {
            get => string.IsNullOrWhiteSpace(NotifyTimesJson) 
                ? new List<string>() 
                : JsonSerializer.Deserialize<List<string>>(NotifyTimesJson) ?? new List<string>();
            set => NotifyTimesJson = JsonSerializer.Serialize(value ?? new List<string>());
        }
    }
}