using BirthdayReminder.Data;
using BirthdayReminder.Implementations;
using BirthdayReminder.interfaces;
using BirthdayReminder.Models;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotificationDomain;
using PD.EmailSender.Helpers.Model;

namespace BirthdayReminder.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BirthdayController : ControllerBase
    {
        private readonly NotificationDbContext _dbContext;
        private readonly IFirebaseStoreService _firebaseStoreService;
        private readonly IPublishEndpoint _publishEndpoint;

        public BirthdayController(
            NotificationDbContext dbContext,
            IFirebaseStoreService firebaseStoreService,
            IPublishEndpoint publishEndpoint)
        {
            _dbContext = dbContext;
            _firebaseStoreService = firebaseStoreService;
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet("reminders")]
        public async Task<IActionResult> GetReminders()
        {
            var celebrants = await _firebaseStoreService.CelebrantsByUserEmailAsync();
            List<MessageModel> messages = new();
            messages.AddRange(_firebaseStoreService.BuildBirthdayMessages(celebrants, true));
            messages.AddRange(_firebaseStoreService.BuildBirthdayMessages(celebrants, false));
            var result = await _firebaseStoreService.SendBirthdayEmails(messages);
            return Ok(result);
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] SubscriptionRequest req)
        {
            if (req == null)
                return BadRequest("Invalid payload");

            if (string.IsNullOrWhiteSpace(req.CelebrantId) || string.IsNullOrWhiteSpace(req.Name))
                return BadRequest("CelebrantId and Name are required");

            if (req.BirthDay < 1 || req.BirthDay > 31 || req.BirthMonth < 1 || req.BirthMonth > 12)
                return BadRequest("Invalid BirthDay or BirthMonth");

            if (req.NotificationTypes == null || req.NotificationTypes.Count == 0)
                return BadRequest("At least one NotificationType is required");

            var subscription = new BirthdaySubscription
            {
                CelebrantId = req.CelebrantId,
                Name = req.Name,
                BirthDay = req.BirthDay,
                BirthMonth = req.BirthMonth,
                NotificationTypes = req.NotificationTypes,
                UserId = req.UserId,
                NotifyTimes = req.NotifyTimes,
                CreatedAt = DateTime.UtcNow
            };

            await _publishEndpoint.Publish(subscription);

            return Accepted("Subscription received and queued for processing");
        }

        [HttpGet("subscription/{celebrantId}")]
        public async Task<IActionResult> GetSubscription(string celebrantId)
        {
            if (string.IsNullOrWhiteSpace(celebrantId))
                return BadRequest("celebrantId is required");

            var sub = await _dbContext.BirthdaySubscriptions
                              .AsNoTracking()
                              .FirstOrDefaultAsync(s => s.CelebrantId == celebrantId);

            if (sub == null)
                return NotFound();

            var result = new
            {
                sub.Id,
                sub.CelebrantId,
                sub.Name,
                sub.BirthDay,
                sub.BirthMonth,
                NotificationTypes = sub.NotificationTypes,
                NotifyTimes = sub.NotifyTimes,
                sub.CreatedAt
            };

            return Ok(result);
        }

        [HttpPost("notifications/subscription")]
        public async Task<IActionResult> SendSubscriptionNotifications(
            [FromServices] ISubscriptionNotificationService notificationService)
        {
            await notificationService.SendSubscriptionNotificationsAsync();
            return Ok("Subscription notifications processed");
        }
    }
}
