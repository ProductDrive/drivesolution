public class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public List<string> Errors { get; set; } = new();
}
