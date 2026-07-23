using BirthdayReminder.Data;
using BirthdayReminder.Implementations;
using BirthdayReminder.interfaces;
using BirthdayReminder.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotificationDomain;

namespace BirthdayReminder.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WhatsAppReminderController : ControllerBase
    {
        private readonly OtpService _otpService;
        private readonly NotificationDbContext _dbContext;
        private readonly IFirebaseStoreService _firebaseStoreService;

        public WhatsAppReminderController(
            OtpService otpService,
            NotificationDbContext dbContext,
            IFirebaseStoreService firebaseStoreService)
        {
            _otpService = otpService;
            _dbContext = dbContext;
            _firebaseStoreService = firebaseStoreService;
        }

        [HttpPost("request-otp")]
        public IActionResult RequestOtp()
        {
            var sessionId = _otpService.RequestOtp();
            return Ok(new OtpResponse { SessionId = sessionId });
        }

        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp([FromBody] OtpVerifyRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SessionId) || string.IsNullOrWhiteSpace(request.Code))
                return BadRequest("SessionId and Code are required");

            var token = _otpService.VerifyOtp(request.SessionId, request.Code);
            if (token == null)
                return Unauthorized("Invalid or expired OTP");

            return Ok(new OtpVerifyResponse { Token = token });
        }

        [HttpGet("subscribers")]
        public async Task<IActionResult> GetSubscribers()
        {
            var token = ExtractBearerToken();
            if (token == null || !_otpService.IsValidToken(token))
                return Unauthorized("Invalid or expired token");

            var allSubscriptions = await _dbContext.BirthdaySubscriptions
                .AsNoTracking()
                .ToListAsync();

            var whatsappSubscriptions = allSubscriptions
                .Where(s => s.NotificationTypes.Contains(NotificationType.WhatsApp))
                .ToList();

            var allUsers = await _firebaseStoreService.GetAllUsers();

            var today = DateTime.Today;
            var results = new List<WhatsAppReminderResponse>();

            foreach (var sub in whatsappSubscriptions)
            {
                var matches = GetUpcomingMatches(sub, today);
                if (matches.Count == 0)
                    continue;

                var user = allUsers.FirstOrDefault(u => u.UserId == sub.UserId);
                if (user == null || string.IsNullOrWhiteSpace(user.WhatsappNumber))
                    continue;

                var existing = results.FirstOrDefault(r => r.UserId == sub.UserId);
                if (existing == null)
                {
                    existing = new WhatsAppReminderResponse
                    {
                        UserId = sub.UserId,
                        UserName = user.Email,
                        WhatsappNumber = user.WhatsappNumber
                    };
                    results.Add(existing);
                }

                foreach (var match in matches)
                {
                    var birthDateThisYear = new DateTime(today.Year, sub.BirthMonth, sub.BirthDay);
                    if (birthDateThisYear < today)
                        birthDateThisYear = birthDateThisYear.AddYears(1);

                    var daysUntil = (birthDateThisYear - today).Days;
                    var timeDesc = FormatNotifyTime(match);

                    existing.Celebrants.Add(new CelebrantReminder
                    {
                        CelebrantId = sub.CelebrantId,
                        Name = sub.Name,
                        BirthDay = sub.BirthDay,
                        BirthMonth = sub.BirthMonth,
                        NotifyTime = match.ToString(),
                        DaysUntilBirthday = daysUntil,
                        Message = $"{sub.Name}'s birthday is coming up {timeDesc}!"
                    });
                }
            }

            return Ok(results);
        }

        private static List<NotifyTime> GetUpcomingMatches(BirthdaySubscription sub, DateTime today)
        {
            var result = new List<NotifyTime>();
            var birthDateThisYear = new DateTime(today.Year, sub.BirthMonth, sub.BirthDay);

            if (birthDateThisYear < today)
                birthDateThisYear = birthDateThisYear.AddYears(1);

            var daysUntil = (birthDateThisYear - today).Days;

            if (sub.NotifyTimes.Contains(NotifyTime.OneMonthBefore) && daysUntil <= 30)
                result.Add(NotifyTime.OneMonthBefore);

            if (sub.NotifyTimes.Contains(NotifyTime.TwoWeeksBefore) && daysUntil <= 14)
                result.Add(NotifyTime.TwoWeeksBefore);

            if (sub.NotifyTimes.Contains(NotifyTime.ThreeDaysBefore) && daysUntil <= 3)
                result.Add(NotifyTime.ThreeDaysBefore);

            return result;
        }

        private static string FormatNotifyTime(NotifyTime notifyTime)
        {
            return notifyTime switch
            {
                NotifyTime.OneMonthBefore => "in 1 month",
                NotifyTime.TwoWeeksBefore => "in 2 weeks",
                NotifyTime.ThreeDaysBefore => "in 3 days",
                _ => "soon"
            };
        }

        private string? ExtractBearerToken()
        {
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader))
                return null;

            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return authHeader["Bearer ".Length..].Trim();

            return null;
        }
    }
}
