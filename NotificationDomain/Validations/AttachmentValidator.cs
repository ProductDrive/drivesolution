using NotificationDomain;

public static class AttachmentValidator
{
    public static ValidationResult Validate(AttachmentDTO a)
    {
        var result = new ValidationResult();

        if (a == null)
        {
            result.Errors.Add("Attachment cannot be null.");
            return result;
        }

        if (string.IsNullOrWhiteSpace(a.FileName))
            result.Errors.Add("Attachment filename is required.");

        if (string.IsNullOrWhiteSpace(a.ContentType))
            result.Errors.Add($"Attachment '{a.FileName}' has missing content type.");

        if (a.FileBytes == null || a.FileBytes.Length == 0)
            result.Errors.Add($"Attachment '{a.FileName}' has no data.");

        return result;
    }
}
