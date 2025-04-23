using Core;
using Godot;
using System;

public partial class Test : Control {
    public override void _Ready() {
        GetNode<Button>("VBoxContainer/Button").Pressed += async () => await SettingsManager.Instance.SaveSettings();
        GetNode<Button>("VBoxContainer/Button2").Pressed += async () => await SettingsManager.Instance.LoadSettings();
        GetNode<LineEdit>("VBoxContainer/LineEdit").TextSubmitted += GetSettingFromPath;

        GetNode<HSlider>("VBoxContainer2/HSlider").ValueChanged += OnValueChanged;
        GetNode<MenuButton>("VBoxContainer2/MenuButton").GetPopup().IdPressed += OnIdPressed;
        GetNode<CheckButton>("VBoxContainer2/CheckButton").Toggled += OnButtonToggled;
        GetNode<LineEdit>("VBoxContainer2/LineEdit").TextSubmitted += OnTextSubmitted;
    }

    private void GetSettingFromPath(string path) {
        try {
            GetNode<Label>("VBoxContainer/Label2").Text = (string)SettingsManager.Instance.GetSetting(path);
        } catch (Exception e) {
            GetNode<Label>("VBoxContainer/Label2").Text = e.Message;
        }
    }

    private async void OnValueChanged(double value) {
        await SettingsManager.Instance.SetSetting("ExampleSettings.master_volume", (float)value);
        GetNode<Label>("VBoxContainer2/Label").Text = "Slider value:" + SettingsManager.Instance.GetSetting("ExampleSettings.master_volume", 1.0f);
    }

    private async void OnIdPressed(long id) {
        await SettingsManager.Instance.SetSetting("ExampleSettings.graphics", (int)id);
        GetNode<Label>("VBoxContainer2/Label2").Text = "Choice:" + SettingsManager.Instance.GetSetting("ExampleSettings.graphics", "ultra");
    }

    private async void OnButtonToggled(bool toggled) {
        await SettingsManager.Instance.SetSetting("ExampleSettings.someboolean", toggled);
        GetNode<Label>("VBoxContainer2/Label3").Text = "Toggled:" + SettingsManager.Instance.GetSetting("ExampleSettings.someboolean");
    }

    private async void OnTextSubmitted(string text) {
        await SettingsManager.Instance.SetSetting("ExampleSettings.somestring", text);
        GetNode<Label>("VBoxContainer2/Label4").Text = "Text:" + SettingsManager.Instance.GetSetting("ExampleSettings.somestring");
    }
}
