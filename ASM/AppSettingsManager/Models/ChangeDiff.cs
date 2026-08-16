namespace AppSettingsManager.Models;

public class ChangeDiff
{
    public string Path { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public bool IsAdded { get; set; }
    public bool IsRemoved { get; set; }
}