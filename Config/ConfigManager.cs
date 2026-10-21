using System;
using System.IO;
using StardewModdingAPI;

namespace ProximityVoiceChat.Config;

/// <summary>Loads and saves the settings file, always falling back to usable defaults.</summary>
public sealed class ConfigManager
{
    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;

    public ConfigManager(IModHelper helper, IMonitor monitor)
    {
        this._helper = helper;
        this._monitor = monitor;
    }

    public string ConfigPath => Path.Combine(this._helper.DirectoryPath, ModConstants.ConfigFileName);

    /// <summary>Load the config, repairing invalid values and reporting unreadable files.</summary>
    public ModConfig Load()
    {
        try
        {
            if (!File.Exists(this.ConfigPath))
            {
                ModConfig defaults = new();
                this.Save(defaults);
                this._monitor.Log("No settings file found; created one with default values.", LogLevel.Info);
                return defaults;
            }

            ModConfig? config = this._helper.Data.ReadJsonFile<ModConfig>(ModConstants.ConfigFileName);
            if (config is null)
            {
                this._monitor.Log("The settings file could not be read; using default settings.", LogLevel.Warn);
                return new ModConfig();
            }

            string requestedTestKey = config.MicrophoneTestKey;
            int requestedRadius = config.ProximityRadius;
            config.Normalize();
            if (requestedRadius != config.ProximityRadius)
                this._monitor.Log($"Voice reach {requestedRadius} is not offered by this version; using {config.ProximityRadius} tiles.", LogLevel.Info);
            if (!string.Equals(requestedTestKey, config.MicrophoneTestKey, StringComparison.OrdinalIgnoreCase))
                this._monitor.Log($"Microphone test key '{requestedTestKey}' is already used by another PVC action; using '{config.MicrophoneTestKey}' instead.", LogLevel.Info);

            return config;
        }
        catch (Exception ex)
        {
            this._monitor.Log($"The settings file is invalid; using default settings. {ex.Message}", LogLevel.Error);
            this.BackupBrokenConfig();

            ModConfig defaults = new();
            this.Save(defaults);
            return defaults;
        }
    }

    /// <summary>Persist the current settings. Never throws.</summary>
    public void Save(ModConfig config)
    {
        try
        {
            config.Normalize();
            this._helper.Data.WriteJsonFile(ModConstants.ConfigFileName, config);
        }
        catch (Exception ex)
        {
            this._monitor.Log($"Could not save the settings file: {ex.Message}", LogLevel.Error);
        }
    }

    private void BackupBrokenConfig()
    {
        try
        {
            string backup = this.ConfigPath + ".broken";
            File.Copy(this.ConfigPath, backup, overwrite: true);
            this._monitor.Log($"Saved a copy of the unreadable settings at '{backup}'.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            this._monitor.Log($"Could not back up the unreadable settings file: {ex.Message}", LogLevel.Trace);
        }
    }
}
