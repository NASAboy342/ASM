using System.Text.Json;
using AppSettingsManager.Models;

namespace AppSettingsManager.Services;

public class SettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly BackupService _backupService;

    public SettingsService(ILogger<SettingsService> logger, BackupService backupService)
    {
        _logger = logger;
        _backupService = backupService;
    }

    public SettingNode ReadSettings(string appSettingsPath)
    {
        try
        {
            var json = File.ReadAllText(appSettingsPath);
            var dictionary = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (dictionary == null)
                throw new InvalidOperationException("Failed to parse JSON");

            var rootNode = new SettingNode
            {
                Key = "Root",
                NodeType = "object",
                IsExpanded = true
            };

            ParseElement(dictionary, rootNode, string.Empty);
            StoreOriginalValues(rootNode);

            return rootNode;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON in file: {Path}", appSettingsPath);
            throw new InvalidOperationException($"Invalid JSON format in {appSettingsPath}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading settings file: {Path}", appSettingsPath);
            throw;
        }
    }

    private void ParseElement(Dictionary<string, JsonElement> element, SettingNode parentNode, string path)
    {
        foreach (var kvp in element)
        {
            var currentPath = string.IsNullOrEmpty(path) ? kvp.Key : $"{path}.{kvp.Key}";

            switch (kvp.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    var nestedDict = kvp.Value.Deserialize<Dictionary<string, JsonElement>>();
                    if (nestedDict is not null)
                    {
                        var objNode = new SettingNode
                        {
                            Key = kvp.Key,
                            NodeType = "object",
                            IsExpanded = false,
                            Path = currentPath
                        };
                        parentNode.Children.Add(objNode);
                        ParseElement(nestedDict, objNode, currentPath);
                    }
                    break;

                case JsonValueKind.Array:
                    var arrNode = new SettingNode
                    {
                        Key = kvp.Key,
                        NodeType = "array",
                        Path = currentPath
                    };
                    parentNode.Children.Add(arrNode);

                    var arrElements = kvp.Value.EnumerateArray().ToList();
                    for (int i = 0; i < arrElements.Count; i++)
                    {
                        var item = arrElements[i];
                        var itemPath = $"{currentPath}[{i}]";

                        switch (item.ValueKind)
                        {
                            case JsonValueKind.Object:
                                var itemDict = item.Deserialize<Dictionary<string, JsonElement>>();
                                if (itemDict is not null)
                                {
                                    var itemObjNode = new SettingNode
                                    {
                                        Key = $"[{i}]",
                                        NodeType = "object",
                                        Path = itemPath
                                    };
                                    arrNode.Children.Add(itemObjNode);
                                    ParseElement(itemDict, itemObjNode, itemPath);
                                }
                                break;

                            case JsonValueKind.Array:
                                var nestedArrNode = new SettingNode
                                {
                                    Key = $"[{i}]",
                                    NodeType = "array",
                                    Path = itemPath
                                };
                                arrNode.Children.Add(nestedArrNode);
                                break;

                            default:
                                arrNode.Children.Add(new SettingNode
                                {
                                    Key = $"[{i}]",
                                    Value = FormatJsonValue(item),
                                    NodeType = GetNodeTypeName(item.ValueKind),
                                    Path = itemPath
                                });
                                break;
                        }
                    }
                    break;

                default:
                    parentNode.Children.Add(new SettingNode
                    {
                        Key = kvp.Key,
                        Value = FormatJsonValue(kvp.Value),
                        NodeType = GetNodeTypeName(kvp.Value.ValueKind),
                        Path = currentPath
                    });
                    break;
            }
        }
    }

    private string FormatJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            _ => element.ToString()
        };
    }

    private string GetNodeTypeName(JsonValueKind kind)
    {
        return kind switch
        {
            JsonValueKind.String => "string",
            JsonValueKind.Number => "number",
            JsonValueKind.True or JsonValueKind.False => "boolean",
            JsonValueKind.Null => "null",
            _ => "unknown"
        };
    }

    private void StoreOriginalValues(SettingNode node)
    {
        foreach (var child in node.Children)
        {
            if (IsPrimitiveType(child.NodeType))
            {
                child.OriginalValue = child.Value;
                child.IsValueChanged = false;
            }
            else
            {
                StoreOriginalValues(child);
            }
        }
    }

    private bool IsPrimitiveType(string type)
    {
        return type is "string" or "number" or "boolean" or "null";
    }

    public List<ChangeDiff> GetChanges(SettingNode rootNode)
    {
        var changes = new List<ChangeDiff>();
        CollectChanges(rootNode, changes);
        return changes;
    }

    private void CollectChanges(SettingNode node, List<ChangeDiff> changes)
    {
        foreach (var child in node.Children)
        {
            if (IsPrimitiveType(child.NodeType))
            {
                if (child.IsValueChanged && child.Path is not null)
                {
                    changes.Add(new ChangeDiff
                    {
                        Path = child.Path,
                        OldValue = child.OriginalValue,
                        NewValue = child.Value,
                        IsAdded = false,
                        IsRemoved = false
                    });
                }
            }
            else
            {
                CollectChanges(child, changes);
            }
        }
    }

    public void SaveSettings(string appSettingsPath, SettingNode rootNode)
    {
        try
        {
            _backupService.CreateBackup(appSettingsPath);
            var dict = BuildDictionaryFromTree(rootNode);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
            };

            var json = JsonSerializer.Serialize(dict, options);
            JsonDocument.Parse(json);
            File.WriteAllText(appSettingsPath, json);

            _logger.LogInformation("Settings saved successfully to: {Path}", appSettingsPath);
        }
        catch (IOException ex) when (ex.Message.Contains("access to the path") || ex.Message.Contains("Permission"))
        {
            _logger.LogError(ex, "Permission error saving file: {Path}", appSettingsPath);
            throw new InvalidOperationException($"Permission denied: {appSettingsPath}", ex);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "File locked or unavailable: {Path}", appSettingsPath);
            throw new InvalidOperationException($"File is locked or unavailable: {appSettingsPath}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings: {Path}", appSettingsPath);
            throw;
        }
    }

    private Dictionary<string, object?> BuildDictionaryFromTree(SettingNode node)
    {
        var dict = new Dictionary<string, object?>();

        foreach (var child in node.Children)
        {
            if (IsPrimitiveType(child.NodeType))
            {
                dict[child.Key] = child.NodeType switch
                {
                    "string" => child.Value,
                    "boolean" => bool.Parse(child.Value ?? "false"),
                    "number" when int.TryParse(child.Value, out var intVal) => intVal,
                    "number" when double.TryParse(child.Value, out var doubleVal) => doubleVal,
                    "null" => null,
                    _ => child.Value
                };
            }
            else
            {
                dict[child.Key] = BuildDictionaryFromTree(child);
            }
        }

        return dict;
    }
}
