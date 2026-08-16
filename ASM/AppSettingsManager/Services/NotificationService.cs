namespace AppSettingsManager.Services;

/// <summary>
/// Service for broadcasting events between components/pages
/// Used for cross-page communication (e.g., auto-refresh when directories change)
/// </summary>
public class NotificationService
{
    private List<Action<string>> _listeners = new();
    private readonly object _lock = new();

    public void RegisterListener(Action<string> listener)
    {
        lock (_lock)
        {
            _listeners.Add(listener);
        }
    }

    public void UnregisterListener(Action<string> listener)
    {
        lock (_lock)
        {
            _listeners.Remove(listener);
        }
    }

    public void Broadcast(string message)
    {
        lock (_lock)
        {
            var copy = _listeners.ToList();
            foreach (var listener in copy)
            {
                try
                {
                    listener.Invoke(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"NotificationService error: {ex.Message}");
                }
            }
        }
    }
}