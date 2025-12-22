using Raspberry.Pi.Dashboard.Domain;

namespace Raspberry.Pi.Dashboard.Services;

public class SettingsService : ISettingsService
{
    private Settings _settings = new();

    public event EventHandler? SettingsChanged;

    public Settings GetSettings()
    {
        return _settings;
    }

    public void ResetToDefaultSettings()
    {
        _settings = new();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateSettings(Action<Settings> updateAction)
    {
        updateAction(_settings);
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}