using Godot;
using System;

public partial class SettingsView : MarginContainer
{
    private const double SaveStatusDisplaySeconds = 2.0;

    private CheckButton _developmentModeCheckBox = null!;
    private Label _saveStatusLabel = null!;
    private int _saveStatusRevision;

    public override void _Ready()
    {
        _developmentModeCheckBox = GetNodeOrNull<CheckButton>(
            "Content/DevelopmentSection/DevelopmentModeCheckBox")
            ?? throw new InvalidOperationException("Development mode control is missing.");
        _saveStatusLabel = GetNodeOrNull<Label>(
            "Content/DevelopmentSection/SaveStatusLabel")
            ?? throw new InvalidOperationException("Settings save status label is missing.");

        _developmentModeCheckBox.SetPressedNoSignal(
            AppServices.SettingsService.Current.DevelopmentMode);
        _developmentModeCheckBox.Toggled += OnDevelopmentModeToggled;
    }

    private void OnDevelopmentModeToggled(bool enabled)
    {
        var settings = AppServices.SettingsService.Current;
        var previousValue = settings.DevelopmentMode;

        try
        {
            settings.DevelopmentMode = enabled;
            AppServices.SettingsService.Save(settings);
            AppServices.Logger.SetDevelopmentMode(enabled);
            AppServices.Logger.Information(
                "Configuration",
                "DevelopmentModeChanged",
                $"Development mode changed: {(enabled ? "enabled" : "disabled")}");
            ShowTemporarySaveStatus("设置已保存");
        }
        catch (Exception exception)
        {
            settings.DevelopmentMode = previousValue;
            _developmentModeCheckBox.SetPressedNoSignal(previousValue);
            _saveStatusLabel.Text = "设置保存失败";
            GD.PushError($"Unable to save application settings ({exception.GetType().Name}).");
        }
    }

    private async void ShowTemporarySaveStatus(string message)
    {
        var revision = ++_saveStatusRevision;
        _saveStatusLabel.Text = message;

        await ToSignal(
            GetTree().CreateTimer(SaveStatusDisplaySeconds),
            SceneTreeTimer.SignalName.Timeout);

        if (revision == _saveStatusRevision && GodotObject.IsInstanceValid(_saveStatusLabel))
        {
            _saveStatusLabel.Text = string.Empty;
        }
    }
}
