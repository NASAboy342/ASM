namespace AppSettingsManager.Models;

/// <summary>
/// Groups projects by their hosting directory for organized display
/// </summary>
public class HostedProjectsGroup
{
    /// <summary>
    /// The hosting directory information
    /// </summary>
    public HostDirectoryInfo Directory { get; set; } = new();

    /// <summary>
    /// List of projects found in this hosting directory
    /// </summary>
    public List<ProjectInfo> Projects { get; set; } = new();

    /// <summary>
    /// Number of environment-specific appsettings files across all projects
    /// </summary>
    public int TotalAppSettingsFiles => Projects.Sum(p => p.AppSettingsFiles.Count);

    /// <summary>
    /// Gets the most recent last modified date among all projects
    /// </summary>
    public DateTime LatestModified => Projects.Max(p => p.LastModified);

    /// <summary>
    /// Gets a summary string for display
    /// </summary>
    public string GetSummary()
    {
        return $"{Directory.GetDisplayName()} • {Projects.Count} project{(Projects.Count != 1 ? "s" : "")} • {TotalAppSettingsFiles} config files";
    }
}