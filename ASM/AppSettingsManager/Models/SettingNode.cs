using System.Text.Json;

namespace AppSettingsManager.Models;

public class SettingNode
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? OriginalValue { get; set; }
    public bool IsValueChanged { get; set; }
    public bool IsExpanded { get; set; }
    public string NodeType { get; set; } = string.Empty;
    public List<SettingNode> Children { get; set; } = [];
    public string? Path { get; set; }
}