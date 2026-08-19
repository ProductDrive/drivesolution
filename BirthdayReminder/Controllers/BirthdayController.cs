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
        private readonly IDeviceTokenService _deviceTokenService;
        private readonly IPushNotificationService _pushNotificationService;
        private readonly FirebaseTokenValidator _firebaseTokenValidator;

        public BirthdayController(
            NotificationDbContext dbContext,
            IFirebaseStoreService firebaseStoreService,
            IPublishEndpoint publishEndpoint,
            IDeviceTokenService deviceTokenService,
            IPushNotificationService pushNotificationService,
            FirebaseTokenValidator firebaseTokenValidator)
        {
            _dbContext = dbContext;
            _firebaseStoreService = firebaseStoreService;
            _publishEndpoint = publishEndpoint;
            _deviceTokenService = deviceTokenService;
            _pushNotificationService = pushNotificationService;
            _firebaseTokenValidator = firebaseTokenValidator;
        }

        /// <summary>
        /// Extracts the Firebase ID token from the Authorization header and returns
        /// the verified account UID, or null when missing/invalid/unverified.
        /// </summary>
        private async Task<string?> GetVerifiedUidAsync()
        {
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader))
                return null;

            string? idToken = null;
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                idToken = authHeader["Bearer ".Length..].Trim();

            return await _firebaseTokenValidator.ValidateAndGetVerifiedUidAsync(idToken);
        }

        [HttpGet("reminders")]
        public async Task<IActionResult> GetReminders()
        {
            // Send email reminders
            var celebrants = await _firebaseStoreService.CelebrantsByUserEmailAsync();
            List<MessageModel> messages = new();
            messages.AddRange(_firebaseStoreService.BuildBirthdayMessages(celebrants, true));
            messages.AddRange(_firebaseStoreService.BuildBirthdayMessages(celebrants, false));
            var emailResult = await _firebaseStoreService.SendBirthdayEmails(messages);

            // Send WhatsApp reminders
            //var whatsappCelebrants = await _firebaseStoreService.CelebrantsByWhatsAppAsync();
            //List<MessageModel> whatsappMessages = new();
            //whatsappMessages.AddRange(_firebaseStoreService.BuildWhatsAppBirthdayMessages(whatsappCelebrants, true));
            //whatsappMessages.AddRange(_firebaseStoreService.BuildWhatsAppBirthdayMessages(whatsappCelebrants, false));
            //var whatsappResult = await _firebaseStoreService.SendBirthdayWhatsApp(whatsappMessages);

            // Send push notifications
            var celebrantsByUser = await _firebaseStoreService.CelebrantsByUserIdAsync();
            foreach (var entry in celebrantsByUser)
            {
                var userId = entry.Key;
                var names = entry.Value.Select(c => c.Name).ToList();
                var body = names.Count == 1
                    ? $"{names[0]} has a birthday today or tomorrow!"
                    : $"{string.Join(", ", names.Take(3))}{(names.Count > 3 ? "..." : "")} have birthdays today or tomorrow!";
                await _pushNotificationService.SendToUserAsync(userId, "Birthday Reminder", body);
            }

            return Ok(new { emailResult});
            //return Ok(new { emailResult, whatsappResult });
        }

        [HttpPost("verify-recaptcha")]
        public async Task<IActionResult> VerifyRecaptcha(
            [FromBody] RecaptchaVerificationRequest req,
            [FromServices] RecaptchaVerifier verifier)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Token))
                return BadRequest("Token is required");

            if (!verifier.IsConfigured)
                return StatusCode(503, "reCAPTCHA is not configured");

            var result = await verifier.VerifyAsync(req.Token);
            if (!result.Valid)
                return StatusCode(403, new { valid = false, score = result.Score, reason = result.InvalidReason });

            return Ok(new { valid = true, score = result.Score });
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

            var userId = await GetVerifiedUidAsync();
            if (userId == null)
                return Unauthorized("A valid, verified account is required");

            var subscription = new BirthdaySubscription
            {
                CelebrantId = req.CelebrantId,
                Name = req.Name,
                BirthDay = req.BirthDay,
                BirthMonth = req.BirthMonth,
                NotificationTypes = req.NotificationTypes,
                UserId = userId,
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

        [HttpPost("register-token")]
        public async Task<IActionResult> RegisterToken([FromBody] RegisterTokenRequest req)
        {
            if (req == null)
                return BadRequest("Invalid payload");

            if (string.IsNullOrWhiteSpace(req.Token))
                return BadRequest("Token is required");

            var userId = await GetVerifiedUidAsync();
            if (userId == null)
                return Unauthorized("A valid, verified account is required");

            await _deviceTokenService.RegisterTokenAsync(userId, req.Token, req.Platform);
            return Ok("Token registered successfully");
        }

        [HttpPost("unregister-token")]
        public async Task<IActionResult> UnregisterToken([FromBody] UnregisterTokenRequest req)
        {
            if (req == null)
                return BadRequest("Invalid payload");

            if (string.IsNullOrWhiteSpace(req.Token))
                return BadRequest("Token is required");

            var userId = await GetVerifiedUidAsync();
            if (userId == null)
                return Unauthorized("A valid, verified account is required");

            await _deviceTokenService.UnregisterTokenAsync(userId, req.Token);
            return Ok("Token unregistered successfully");
        }

        [HttpPost("request-deletion")]
        public async Task<IActionResult> RequestDeletion(
            [FromBody] DeletionRequest req,
            [FromServices] IDeletionRequestService deletionService)
        {
            if (req == null)
                return BadRequest("Invalid payload");

            if (string.IsNullOrWhiteSpace(req.Email))
                return BadRequest("Email is required");

            var userId = await GetVerifiedUidAsync();
            if (userId == null)
                return Unauthorized("A valid, verified account is required");

            req.UserId = userId;

            await deletionService.SendDeletionRequestEmailAsync(req);

            return Ok("Account deletion request submitted. Our team will process it shortly.");
        }
    }
}
