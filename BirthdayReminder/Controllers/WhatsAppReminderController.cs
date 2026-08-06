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

            var whatsappSubscriptions = await _dbContext.BirthdaySubscriptions
                .AsNoTracking()
                .Where(s => s.NotificationTypesJson.Contains("3"))
                .ToListAsync();

            var allUsers = await _firebaseStoreService.GetAllUsers();
            var allCelebrants = await _firebaseStoreService.GetAllCelebrant();

            var today = DateTime.Today;
            var userById = allUsers
                .GroupBy(u => u.UserId)
                .ToDictionary(g => g.Key, g => g.First());

            var results = new Dictionary<string, WhatsAppReminderResponse>();

            foreach (var sub in whatsappSubscriptions)
            {
                var birthDateThisYear = GetBirthDateThisYear(sub.BirthDay, sub.BirthMonth, today);
                var daysUntil = (birthDateThisYear - today).Days;

                var upcomingTimes = GetUpcomingMatches(sub, daysUntil);
                if (upcomingTimes.Count == 0)
                    continue;

                if (!results.TryGetValue(sub.UserId, out var existing))
                {
                    existing = CreateReminderResponse(sub.UserId, userById);
                    results[sub.UserId] = existing;
                }

                foreach (var match in upcomingTimes)
                {
                    existing.Celebrants.Add(new CelebrantReminder
                    {
                        CelebrantId = sub.CelebrantId,
                        Name = sub.Name,
                        BirthDay = sub.BirthDay,
                        BirthMonth = sub.BirthMonth,
                        NotifyTime = match.ToString(),
                        DaysUntilBirthday = daysUntil,
                        Message = $"{sub.Name}'s birthday is coming up {FormatNotifyTime(match)}!"
                    });
                }
            }

            var celebrantsWithBirthdayToday = allCelebrants
                .Where(c => c.BirthMonth == today.Month && c.BirthDay == today.Day)
                .GroupBy(c => c.UserId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var group in celebrantsWithBirthdayToday)
            {
                if (!userById.TryGetValue(group.Key, out var user))
                    continue;

                if (string.IsNullOrWhiteSpace(user.WhatsappNumber))
                    continue;

                if (!results.TryGetValue(group.Key, out var existing))
                {
                    existing = CreateReminderResponse(group.Key, userById);
                    results[group.Key] = existing;
                }

                var addedCelebrantIds = existing.Celebrants.Select(c => c.CelebrantId).ToHashSet();

                foreach (var celebrant in group.Value)
                {
                    if (!addedCelebrantIds.Add(celebrant.Id))
                        continue;

                    existing.Celebrants.Add(new CelebrantReminder
                    {
                        CelebrantId = celebrant.Id,
                        Name = celebrant.Name,
                        BirthDay = celebrant.BirthDay,
                        BirthMonth = celebrant.BirthMonth,
                        NotifyTime = "Today",
                        DaysUntilBirthday = 0,
                        Message = $"{celebrant.Name}'s birthday is today!"
                    });
                }
            }

            return Ok(results.Values);
        }

        private static List<NotifyTime> GetUpcomingMatches(BirthdaySubscription sub, int daysUntil)
        {
            var result = new List<NotifyTime>();

            if (sub.NotifyTimes.Contains(NotifyTime.OneMonthBefore) && daysUntil == 30)
                result.Add(NotifyTime.OneMonthBefore);

            if (sub.NotifyTimes.Contains(NotifyTime.TwoWeeksBefore) && daysUntil == 14)
                result.Add(NotifyTime.TwoWeeksBefore);

            if (sub.NotifyTimes.Contains(NotifyTime.ThreeDaysBefore) && daysUntil == 3)
                result.Add(NotifyTime.ThreeDaysBefore);

            return result;
        }

        private static DateTime GetBirthDateThisYear(int birthDay, int birthMonth, DateTime today)
        {
            var birthDate = new DateTime(today.Year, birthMonth, birthDay);
            if (birthDate < today)
                birthDate = birthDate.AddYears(1);
            return birthDate;
        }

        private static WhatsAppReminderResponse CreateReminderResponse(string userId, Dictionary<string, UserRecord> userById)
        {
            userById.TryGetValue(userId, out var user);
            return new WhatsAppReminderResponse
            {
                UserId = userId,
                UserName = user?.Email ?? userId,
                WhatsappNumber = user?.WhatsappNumber ?? ""
            };
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
