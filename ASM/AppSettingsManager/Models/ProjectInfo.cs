namespace AppSettingsManager.Models;

public class ProjectInfo
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string AppSettingsPath { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public string Environment { get; set; } = "Production";
    
    /// <summary>
    /// List of all available appsettings files for this project (e.g., appsettings.json, appsettings.Development.json, etc.)
    /// </summary>
    public Dictionary<string, string> AppSettingsFiles { get; set; } = new();
    
    /// <summary>
    /// Gets the current selected appsettings file path (defaults to base appsettings.json)
    /// </summary>
    public string GetSelectedFile(string? environment = null)
    {
        if (string.IsNullOrEmpty(environment))
            environment = Environment;
        
        // Check if there's a specific environment file
        var envKey = GetFileKeyForEnvironment(environment);
        if (AppSettingsFiles.ContainsKey(envKey))
            return AppSettingsFiles[envKey];
        
        // Fall back to base appsettings.json
        if (AppSettingsFiles.ContainsKey("appsettings.json"))
            return AppSettingsFiles["appsettings.json"];
        
        return AppSettingsPath;
    }
    
    /// <summary>
    /// Gets the file key for a given environment name
    /// </summary>
    private string GetFileKeyForEnvironment(string environment)
    {
        var env = environment.Trim().ToLower();
        if (env == "production") return "appsettings.Production.json";
        if (env == "staging") return "appsettings.Staging.json";
        if (env == "development") return "appsettings.Development.json";
        if (env == "demo") return "appsettings.Demo.json";
        if (env == "productionsa") return "appsettings.ProductionSA.json";
        return "appsettings.json";
    }
}