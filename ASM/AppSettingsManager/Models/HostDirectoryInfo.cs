namespace AppSettingsManager.Models;

/// <summary>
/// Represents a hosting directory that contains website projects
/// </summary>
public class HostDirectoryInfo
{
    /// <summary>
    /// The file system path to the hosting directory
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the directory (can be customized)
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the directory section is expanded in the UI
    /// </summary>
    public bool IsExpanded { get; set; } = true;

    /// <summary>
    /// Number of projects found in this directory
    /// </summary>
    public int ProjectCount { get; set; } = 0;

    /// <summary>
    /// When the directory was added to the configuration
    /// </summary>
    public DateTime AddedDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets a friendly display name for the directory if DisplayName is empty
    /// </summary>
    public string GetDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(DisplayName))
            return DisplayName;
        
        return Path.Split('/').LastOrDefault() ?? Path;
    }

    /// <summary>
    /// Gets the folder icon based on directory name or environment type
    /// </summary>
    public string GetFolderIcon()
    {
        var pathLower = Path.ToLower();
        
        if (pathLower.Contains("production") || pathLower.Contains("prod"))
            return "🟢";
        if (pathLower.Contains("staging") || pathLower.Contains("stage"))
            return "🟡";
        if (pathLower.Contains("development") || pathLower.Contains("dev"))
            return "🔵";
        
        return "📁";
    }
}