using Raspberry.Pi.Dashboard.Domain;

namespace Raspberry.Pi.Dashboard.Services;

public interface ISettingsService
{
    Settings GetSettings();
    void UpdateSettings(Action<Settings> updateAction);
    void ResetToDefaultSettings();
    event EventHandler? SettingsChanged;
}
