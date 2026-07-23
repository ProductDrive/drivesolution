using BirthdayReminder.Models;
using Microsoft.Extensions.Caching.Distributed;
using PD.EmailSender.Helpers;
using PD.EmailSender.Helpers.Model;
using System.Collections.Concurrent;

namespace BirthdayReminder.Implementations
{
    public class OtpService
    {
        private readonly ConcurrentDictionary<string, (string Code, DateTime Expiry)> _otpStore = new();
        private readonly IDistributedCache _cache;

        private const int OtpLength = 6;
        private static readonly TimeSpan OtpExpiry = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan TokenExpiry = TimeSpan.FromHours(1);

        private const string OtpEmailAddress = "afeexclusive@gmail.com";
        private const string TokenPrefix = "otp_token:";

        public OtpService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public string RequestOtp()
        {
            var sessionId = Guid.NewGuid().ToString("N");
            var code = GenerateOtpCode();

            _otpStore[sessionId] = (code, DateTime.UtcNow.Add(OtpExpiry));

            _ = SendOtpEmailAsync(code);

            return sessionId;
        }

        public string? VerifyOtp(string sessionId, string code)
        {
            if (!_otpStore.TryRemove(sessionId, out var stored))
                return null;

            if (DateTime.UtcNow > stored.Expiry)
                return null;

            if (stored.Code != code)
                return null;

            var token = Guid.NewGuid().ToString("N");
            _cache.SetString(
                $"{TokenPrefix}{token}",
                "1",
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.Add(TokenExpiry)
                });

            return token;
        }

        public bool IsValidToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var value = _cache.GetString($"{TokenPrefix}{token}");
            return value != null;
        }

        private static string GenerateOtpCode()
        {
            return Random.Shared.Next(0, 1_000_000).ToString("D6");
        }

        private static async Task SendOtpEmailAsync(string code)
        {
            try
            {
                var messageDto = new MessageModel
                {
                    Contacts = new List<ContactsModel>
                    {
                        new() { Email = OtpEmailAddress }
                    },
                    Subject = "Your WhatsApp Reminder Dashboard OTP",
                    Message = $"""
                        <h2>OTP Verification</h2>
                        <p>Your one-time password is:</p>
                        <h1 style="letter-spacing:8px;font-size:32px;color:#2563eb;">{code}</h1>
                        <p>This code expires in 5 minutes.</p>
                        <p>If you did not request this, please ignore this email.</p>
                    """,
                    SenderSettings = new SenderSettingsDTO { OnBehalf = true },
                    FallBackSenderSettings = new SenderSettingsDTO { OnBehalf = true },
                    EmailDisplayName = "Birthday Reminder"
                };

                await SendMailVTwo.SendSingleEmailOnBehalf(messageDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send OTP email: {ex.Message}");
            }
        }
    }
}
