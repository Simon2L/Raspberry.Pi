namespace Raspberry.Pi.Dashboard;

public class Settings
{
    public List<int> Section1 { get; set; } = [1, 2, 3, 4, 5, 6];
    public List<int> Section2 { get; set; } = [8, 9, 10, 11, 12, 13, 14];

    public TimeSpan SmoothDuration { get; set; } = TimeSpan.FromMilliseconds(5_000);
    public TimeSpan HoldDuration { get; set; } = TimeSpan.FromMilliseconds(5_000);
    public TimeSpan SensorDelay { get; set; } = TimeSpan.FromMilliseconds(1_000);
    public int ProximityEventTreshold { get; set; } = 3_000;

    public int Steps { get; set; } = 1;
    public int MaxBrightness { get; set; } = 100;
    public int MinBrightness { get; set; } = 1;
}

public interface ISettingsService
{
    Settings GetSettings();
    void UpdateSettings(Action<Settings> updateAction);
    event EventHandler? SettingsChanged;
}

public class SettingsService : ISettingsService
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Settings _settings = new();

    public event EventHandler? SettingsChanged;

    public Settings GetSettings()
    {
        return _settings;
        /*
        try
        {
            _lock.EnterReadLock();
            var settingsCopy = _settings;
            return settingsCopy;
        }
        finally
        {
            _lock.ExitReadLock();
        }
        */
    }

    public void UpdateSettings(Action<Settings> updateAction)
    {
        updateAction(_settings);
        SettingsChanged?.Invoke(this, EventArgs.Empty);
        /*
        try
        {
            _lock.EnterWriteLock();
            updateAction(_settings);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
        */
    }
}