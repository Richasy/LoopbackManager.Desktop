using Microsoft.Windows.Storage;

namespace LoopbackManager.Shell.Toolkits;

internal static class SettingsToolkit
{
    /// <summary>
    /// Read local setting.
    /// </summary>
    /// <param name="settingName">Setting name.</param>
    /// <param name="defaultValue">Default value provided when the setting does not exist.</param>
    /// <returns>Setting value obtained.</returns>
    public static string ReadLocalSetting(string settingName, string defaultValue)
    {
        var values = GetSettingContainer().Values;
        return values.TryGetValue(settingName, out var value) && value is string text ? text : defaultValue;
    }

    /// <summary>
    /// Write local setting.
    /// </summary>
    /// <param name="settingName">Setting name.</param>
    /// <param name="value">Setting value.</param>
    public static void WriteLocalSetting(string settingName, string value)
        => GetSettingContainer().Values[settingName] = value;

    private static ApplicationDataContainer GetSettingContainer()
    {
#if DEBUG
        return ApplicationData.GetForUnpackaged("Richasy", "LoopbackManager").LocalSettings.CreateContainer("v1", ApplicationDataCreateDisposition.Always);
#else
        return ApplicationData.GetDefault().LocalSettings.CreateContainer("v1", ApplicationDataCreateDisposition.Always);
#endif
    }
}
