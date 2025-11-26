using NotificationDomain;

public static class SenderSettingsValidator
{
    public static ValidationResult Validate(SenderSettingsDTO sender)
    {
        var result = new ValidationResult();

        if (sender == null)
        {
            result.Errors.Add("Sender settings are missing.");
            return result;
        }

        if (string.IsNullOrWhiteSpace(sender.Email))
            result.Errors.Add("Sender email is required.");
        else if (!EmailHelper.IsValidEmail(sender.Email))
            result.Errors.Add($"Invalid sender email format: {sender.Email}");

        if (string.IsNullOrWhiteSpace(sender.Domain))
            result.Errors.Add("SMTP domain is required.");

        if (sender.Port <= 0)
            result.Errors.Add("SMTP port must be greater than 0.");

        if (string.IsNullOrWhiteSpace(sender.Password))
            result.Errors.Add("SMTP password is required.");

        return result;
    }
}
