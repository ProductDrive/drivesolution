using FirebaseAdmin.Auth;

namespace BirthdayReminder.Implementations
{
    /// <summary>
    /// Verifies a Firebase ID token and returns the UID only when the account
    /// has a verified email. Used to reject unverified/bot accounts on
    /// privileged API endpoints.
    /// </summary>
    public class FirebaseTokenValidator
    {
        /// <summary>
        /// Verifies the ID token signature/expiry (via FirebaseAuth) and confirms
        /// the owning account has a verified email.
        /// Returns the verified UID, or null when invalid/expired/unverified.
        /// </summary>
        public async Task<string?> ValidateAndGetVerifiedUidAsync(string? idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
                return null;

            try
            {
                var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
                if (string.IsNullOrWhiteSpace(decoded.Uid))
                    return null;

                var user = await FirebaseAuth.DefaultInstance.GetUserAsync(decoded.Uid);
                if (user == null || !user.EmailVerified)
                    return null;

                return decoded.Uid;
            }
            catch (FirebaseAuthException)
            {
                return null;
            }
        }
    }
}
