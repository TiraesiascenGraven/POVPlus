using System;
using System.IO;
using Dalamud.Configuration;
using Dalamud.Interface.ImGuiNotification;

namespace Hypostasis.Dalamud;

public abstract class PluginConfiguration
{
    public static DirectoryInfo ConfigFolder => DalamudApi.PluginInterface.ConfigDirectory;
    public static FileInfo ConfigFile => DalamudApi.PluginInterface.ConfigFile;

    public Version PluginVersion;

    protected PluginConfiguration()
    {
        if (this is not IPluginConfiguration)
            throw new ApplicationException("A PluginConfiguration MUST implement IPluginConfiguration!");
    }

    public virtual void Initialize() { }

    public static T LoadConfig<T>() where T : PluginConfiguration, new()
    {
        T config;

        try
        {
            config = DalamudApi.PluginInterface.GetPluginConfig() as T ?? new T();
        }
        catch (Exception e)
        {
            const string message = "Error loading config! Renaming old file and resetting...";
            DalamudApi.ShowNotification(message, NotificationType.Error, 10_000);
            DalamudApi.LogError(message, e);
            config = ResetConfig<T>();
        }

        config.Initialize();
        config.UpdateVersion();

        return config;
    }

    public void Save()
    {
        PluginModuleManager.CheckModules();
        DalamudApi.PluginInterface.SavePluginConfig((IPluginConfiguration)this);
    }

    private static T ResetConfig<T>() where T : new()
    {
        ConfigFile.MoveTo(ConfigFile.FullName + ".CORRUPT", true);
        return new T();
    }

    private void UpdateVersion()
    {
        var pluginVersion = Util.AssemblyName.Version;
        if (PluginVersion == pluginVersion) return;

        var prevVersion = PluginVersion;
        PluginVersion = pluginVersion;

        if (prevVersion < pluginVersion)
            OnUpdate(prevVersion);
    }

    protected virtual void OnUpdate(Version previousVersion) { }
}