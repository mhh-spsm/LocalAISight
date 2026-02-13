namespace LocalAISight.Models
{
    public class Profile
    {
        public string Name { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string DefaultPrompt { get; set; } = string.Empty;
        public string OCRPrompt { get; set; } = string.Empty;
        public override string ToString() => Name ?? string.Empty;
    }
}