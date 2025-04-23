#if TOOLS
using Godot;
using System;

[Tool]
public partial class SMPlugin : EditorPlugin
{
	public override void _EnterTree()
	{
		AddAutoloadSingleton("SettingsManager", "res://addons/nishimaaku.settingsmanager/SettingsManager.cs");
	}

	public override void _ExitTree()
	{
		try {
			RemoveAutoloadSingleton("SettingsManager");
		} catch (Exception e) {
			GD.PushError("Unable to remove a singleton: " + e.Message);
		}
	}
}
#endif
