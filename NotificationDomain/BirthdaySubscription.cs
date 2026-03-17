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
        public string UserId { get; set; } = string.Empty;
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
        public List<NotificationType> NotificationTypes
        {
            get => string.IsNullOrWhiteSpace(NotificationTypesJson) 
                ? new List<NotificationType>() 
                : JsonSerializer.Deserialize<List<NotificationType>>(NotificationTypesJson) ?? new List<NotificationType>();
            set => NotificationTypesJson = JsonSerializer.Serialize(value ?? new List<NotificationType>());
        }
        [NotMapped]
        public List<NotifyTime> NotifyTimes
        {
            get => string.IsNullOrWhiteSpace(NotifyTimesJson) 
                ? new List<NotifyTime>() 
                : JsonSerializer.Deserialize<List<NotifyTime>>(NotifyTimesJson) ?? new List<NotifyTime>();
            set => NotifyTimesJson = JsonSerializer.Serialize(value ?? new List<NotifyTime>());
        }
    }

    public enum NotificationType
    {
        Email,
        SMS,
        Push
    }

    public enum NotifyTime
    {
        OneMonthBefore,
        TwoWeeksBefore,
        ThreeDaysBefore,
    }
}