# GodotSettingsManager [Experimental]

A robust and extensible C# plugin for Godot 4.x that simplifies the management and persistence of user settings. It includes hierarchical settings with categories, encryption, automatic saving/loading, and default value fallbacks.

Key Features:
- Expandable default settings dictionary for easy customization
- Hierarchical structure with support for categories and subcategories
- JSON-based encrypted storage with automatic serialization/deserialization
- Default value fallback for missing or corrupted settings
- Autosaves settings automatically on game exit

# Installation
1. Copy `addons/nishimaaku.settingsmanager` to your Godot project or install it from the Asset Library,
2. Enable the plugin in your Project Settings > Plugins

# Basic Usage
```csharp
// Getting settings
float volume = (float)SettingsManager.Instance.GetSetting("ExampleSettings.master_volume", 0.5f);
string graphicsQuality = (string)SettingsManager.Instance.GetSetting("ExampleSettings.graphics", "medium");
bool someSetting = (bool)SettingsManager.Instance.GetSetting("ExampleSettings.someboolean", false);

// Setting values
await SettingsManager.Instance.SetSetting("ExampleSettings.master_volume", 0.8f);
await SettingsManager.Instance.SetSetting("ExampleSettings.graphics", "ultra");
await SettingsManager.Instance.SetSetting("ExampleSettings.someboolean", true);

// Creating new categories (better extend the DefaultSettings dictionary in SettingsManager.cs for best experience)
// This will automatically create the "GameProgress" category if it doesn't exist
await Settings.Instance.SetSetting("GameProgress.level", 5);
await Settings.Instance.SetSetting("GameProgress.score", 10000);
await Settings.Instance.SetSetting("GameProgress.achievements.first_win", true);
```

# Example
```csharp
using Godot;
using System;

public partial class GameManager : Node
{
    public override void _Ready()
    {
        // Load player preferences
        float musicVolume = (float)SettingsManager.Instance.GetSetting("Audio.music_volume", 0.8f);
        float sfxVolume = (float)SettingsManager.Instance.GetSetting("Audio.sfx_volume", 1.0f);
        
        // Apply settings
        AudioServer.SetBusVolumeDb(
            AudioServer.GetBusIndex("Music"), 
            Mathf.LinearToDb(musicVolume)
        );
        
        AudioServer.SetBusVolumeDb(
            AudioServer.GetBusIndex("SFX"), 
            Mathf.LinearToDb(sfxVolume)
        );
    }
    
    public async void SaveUserPreferences(float musicVol, float sfxVol)
    {
        await SettingsManager.Instance.SetSetting("Audio.music_volume", musicVol);
        await SettingsManager.Instance.SetSetting("Audio.sfx_volume", sfxVol);
        GD.Print("Settings saved successfully");
    }
}
```
