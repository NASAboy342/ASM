using Microsoft.Extensions.Configuration;
using System.Text.Json;
using AppSettingsManager.Models;

namespace AppSettingsManager.Services;

public class AppConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly string _appSettingsPath;
    private const string HostDirectoriesKey = "HostDirectories";
    private const string OldBaseDirectoryKey = "BaseDirectory";

    public AppConfigurationService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _appSettingsPath = Path.Combine(environment.ContentRootPath, "appsettings.json");
    }

    /// <summary>
    /// Gets the list of configured host directories - reads directly from file to avoid cache issues
    /// </summary>
    public List<HostDirectoryInfo> GetHostDirectories()
    {
        try
        {
            // Read directly from the appsettings.json file to avoid IConfiguration caching
            var json = File.ReadAllText(_appSettingsPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            if (root.TryGetProperty(HostDirectoriesKey, out var hostDirsElement) && 
                TryParseHostDirectories(hostDirsElement, out var directories))
            {
                return directories;
            }

            // Fallback to old single BaseDirectory configuration using JsonDocument
            var doc2 = JsonDocument.Parse(json);
            var oldPath = doc2.RootElement.TryGetProperty(OldBaseDirectoryKey, out var oldBaseElement) 
                ? (oldBaseElement.GetString() ?? null) 
                : null;
            doc2.Dispose();
            
            if (!string.IsNullOrEmpty(oldPath) && Directory.Exists(oldPath))
            {
                return new List<HostDirectoryInfo>
                {
                    new()
                    {
                        Path = oldPath,
                        DisplayName = Path.GetFileName(oldPath) ?? oldPath,
                        IsExpanded = true,
                        AddedDate = DateTime.Now
                    }
                };
            }

            return new List<HostDirectoryInfo>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading host directories: {ex.Message}");
            return new List<HostDirectoryInfo>();
        }
    }
    
    /// <summary>
    /// Helper to deserialize a JsonElement as a list of HostDirectoryInfo
    /// </summary>
    private static bool TryParseHostDirectories(JsonElement element, out List<HostDirectoryInfo> directories)
    {
        directories = null!;
        try
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                directories = JsonSerializer.Deserialize<List<HostDirectoryInfo>>(element.GetRawText());
                return directories != null && directories.Count > 0;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Saves the list of host directories to appsettings.json
    /// </summary>
    public void SaveHostDirectories(List<HostDirectoryInfo> directories)
    {
        try
        {
            var json = File.ReadAllText(_appSettingsPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Parse into mutable dictionary
            var config = new Dictionary<string, JsonElement>();
            foreach (var property in root.EnumerateObject())
            {
                config[property.Name] = property.Value;
            }

            // Convert directories to JsonElement
            var optionsSerialize = new JsonSerializerOptions { WriteIndented = false };
            var directoriesJson = JsonSerializer.SerializeToElement(directories, optionsSerialize);
            
            // Update or add HostDirectories
            config[HostDirectoriesKey] = directoriesJson;

            // Remove old BaseDirectory key if it exists
            if (config.ContainsKey(OldBaseDirectoryKey))
            {
                config.Remove(OldBaseDirectoryKey);
            }

            // Write back with formatting
            var optionsWrite = new JsonSerializerOptions { WriteIndented = true };
            var updatedJson = JsonSerializer.Serialize(config, optionsWrite);
            File.WriteAllText(_appSettingsPath, updatedJson);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save host directories: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Adds a new host directory to the configuration
    /// </summary>
    public bool AddHostDirectory(string path, string? displayName = null)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Directory does not exist: {path}");
        }

        var directories = GetHostDirectories();
        
        // Check if directory already exists
        if (directories.Any(d => d.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
        {
            return false; // Already exists
        }

        directories.Add(new HostDirectoryInfo
        {
            Path = path,
            DisplayName = !string.IsNullOrEmpty(displayName) ? displayName : Path.GetFileName(path),
            IsExpanded = true,
            AddedDate = DateTime.Now
        });

        SaveHostDirectories(directories);
        return true;
    }

    /// <summary>
    /// Removes a host directory from the configuration
    /// </summary>
    public bool RemoveHostDirectory(string path)
    {
        var directories = GetHostDirectories();
        var removed = directories.RemoveAll(d => d.Path.Equals(path, StringComparison.OrdinalIgnoreCase)) > 0;

        if (removed)
        {
            SaveHostDirectories(directories);
        }

        return removed;
    }

    /// <summary>
    /// Toggles the expanded state of a directory
    /// </summary>
    public void ToggleDirectoryExpanded(string path, bool isExpanded)
    {
        var directories = GetHostDirectories();
        var directory = directories.FirstOrDefault(d => d.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
        
        if (directory != null)
        {
            directory.IsExpanded = isExpanded;
            SaveHostDirectories(directories);
        }
    }

    /// <summary>
    /// Gets subdirectories within a parent directory for browsing
    /// </summary>
    public List<string> GetSubDirectories(string parentPath)
    {
        if (!Directory.Exists(parentPath))
        {
            return new List<string>();
        }

        return Directory.GetDirectories(parentPath)
            .Select(d => new DirectoryInfo(d).Name)
            .OrderBy(name => name)
            .ToList();
    }

    /// <summary>
    /// Gets parent directories for navigation
    /// </summary>
    public List<string> GetParentDirectories(string path)
    {
        var directories = new List<string>();
        var currentPath = new DirectoryInfo(path);

        while (currentPath.Parent != null)
        {
            directories.Add(currentPath.FullName);
            currentPath = currentPath.Parent;
            
            // Prevent infinite loop
            if (directories.Count > 20)
                break;
        }

        return directories;
    }

    /// <summary>
    /// Validates if a directory exists and is accessible
    /// </summary>
    public bool ValidateDirectoryExists(string path)
    {
        return Directory.Exists(path);
    }
}