using System.IO;

namespace AppSettingsManager.Services;

public class BackupService
{
    private readonly ILogger<BackupService> _logger;

    public BackupService(ILogger<BackupService> logger)
    {
        _logger = logger;
    }

    public string CreateBackup(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found", filePath);
            }

            var backupPath = filePath + ".bak";
            File.Copy(filePath, backupPath, true);

            _logger.LogInformation("Backup created at: {BackupPath}", backupPath);
            return backupPath;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Failed to create backup for: {Path}", filePath);
            throw new InvalidOperationException($"Failed to create backup: {filePath}", ex);
        }
    }

    public bool RestoreFromBackup(string filePath)
    {
        try
        {
            var backupPath = filePath + ".bak";
            
            if (!File.Exists(backupPath))
            {
                _logger.LogWarning("No backup found for: {Path}", filePath);
                return false;
            }

            File.Copy(backupPath, filePath, true);
            _logger.LogInformation("Restored from backup: {BackupPath}", backupPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore from backup for: {Path}", filePath);
            return false;
        }
    }
}