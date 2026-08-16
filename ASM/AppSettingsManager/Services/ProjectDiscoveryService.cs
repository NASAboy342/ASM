using AppSettingsManager.Models;

namespace AppSettingsManager.Services;

public class ProjectDiscoveryService
{
    private readonly AppConfigurationService _configurationService;
    private readonly ILogger<ProjectDiscoveryService> _logger;

    public ProjectDiscoveryService(AppConfigurationService configurationService, ILogger<ProjectDiscoveryService> logger)
    {
        _configurationService = configurationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets projects grouped by their hosting directory
    /// </summary>
    public async Task<List<HostedProjectsGroup>> DiscoverProjectsGroupedByDirectory()
    {
        var groups = new List<HostedProjectsGroup>();
        var hostDirectories = _configurationService.GetHostDirectories();

        if (hostDirectories == null || hostDirectories.Count == 0)
        {
            _logger.LogWarning("No host directories configured");
            return groups;
        }

        foreach (var hostDir in hostDirectories)
        {
            try
            {
                if (!Directory.Exists(hostDir.Path))
                {
                    _logger.LogWarning("Host directory does not exist: {Directory}", hostDir.Path);
                    continue;
                }

                var projects = await Task.Run(() => DiscoverProjectsInDirectory(hostDir.Path));
                
                if (projects.Count > 0)
                {
                    groups.Add(new HostedProjectsGroup
                    {
                        Directory = hostDir,
                        Projects = projects
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning host directory: {Directory}", hostDir.Path);
            }
        }

        return groups.OrderBy(g => g.Directory.GetDisplayName()).ToList();
    }

    /// <summary>
    /// Discovers projects (legacy method for backward compatibility)
    /// </summary>
    public async Task<List<ProjectInfo>> DiscoverProjects()
    {
        var groups = await DiscoverProjectsGroupedByDirectory();
        return groups.SelectMany(g => g.Projects).ToList();
    }

    /// <summary>
    /// Discovers projects in a specific directory
    /// </summary>
    private List<ProjectInfo> DiscoverProjectsInDirectory(string baseDirectory)
    {
        var projects = new List<ProjectInfo>();
        var directories = Directory.GetDirectories(baseDirectory);

        foreach (var dir in directories)
        {
            try
            {
                var projectName = Path.GetFileName(dir);
                var environment = DetermineEnvironment(dir);
                
                // Discover all appsettings*.json files in the project directory
                var appSettingsFiles = new Dictionary<string, string>();
                var baseAppSettingsPath = Path.Combine(dir, "appsettings.json");
                
                if (File.Exists(baseAppSettingsPath))
                {
                    appSettingsFiles["appsettings.json"] = baseAppSettingsPath;
                }
                
                // Find all environment-specific appsettings files
                var envFiles = Directory.GetFiles(dir, "appsettings.*.json");
                foreach (var envFile in envFiles)
                {
                    var fileName = Path.GetFileName(envFile);
                    appSettingsFiles[fileName] = envFile;
                }
                
                if (appSettingsFiles.Count > 0)
                {
                    var primaryPath = appSettingsFiles.ContainsKey("appsettings.json") 
                        ? appSettingsFiles["appsettings.json"] 
                        : appSettingsFiles.Values.First();
                    var lastModified = File.GetLastWriteTime(primaryPath);

                    projects.Add(new ProjectInfo
                    {
                        Name = projectName,
                        Path = dir,
                        AppSettingsPath = primaryPath,
                        LastModified = lastModified,
                        Environment = environment,
                        AppSettingsFiles = appSettingsFiles
                    });

                    _logger.LogInformation("Discovered project: {ProjectName} at {Path} with {FileCount} appsettings files", 
                        projectName, dir, appSettingsFiles.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error discovering project in directory: {Directory}", dir);
            }
        }

        return projects.OrderBy(p => p.Name).ToList();
    }

    private string DetermineEnvironment(string projectPath)
    {
        var pathLower = projectPath.ToLower();
        
        if (pathLower.Contains("production") || pathLower.Contains("prod"))
            return "Production";
        if (pathLower.Contains("staging") || pathLower.Contains("stage"))
            return "Staging";
        if (pathLower.Contains("development") || pathLower.Contains("dev"))
            return "Development";
        
        return "Production";
    }
}