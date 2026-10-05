using System;
using System.Linq;
using Microsoft.Toolkit.Mvvm.Messaging;
using SS14.Launcher.Models.Data;
using SS14.Launcher.ViewModels;

namespace SS14.Launcher.Models.Helix;

public sealed class HelixDiscordPresenceController :
    IRecipient<HelixGameStartedMessage>,
    IRecipient<HelixGameExitedMessage>,
    IRecipient<HelixDiscordRichPresenceSettingsChanged>
{
    private readonly MainWindowViewModel _windowVm;
    private bool _gameRunning = HelixGameActivity.IsGameRunning;

    public HelixDiscordPresenceController(MainWindowViewModel windowVm)
    {
        _windowVm = windowVm;
        WeakReferenceMessenger.Default.RegisterAll(this);

        _windowVm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(MainWindowViewModel.SelectedIndex)
                or nameof(MainWindowViewModel.LoggedIn)
                or nameof(MainWindowViewModel.ConnectingVM)
                or nameof(MainWindowViewModel.BusyTask))
            {
                UpdateDiscordPresence();
            }
        };

        HelixDiscordRichPresence.Instance.SetActivity("Starting launcher");
        UpdateDiscordPresence();
    }

    public void Receive(HelixGameStartedMessage message)
    {
        _gameRunning = true;
        UpdateDiscordPresence();
    }

    public void Receive(HelixGameExitedMessage message)
    {
        _gameRunning = false;
        UpdateDiscordPresence();
    }

    public void Receive(HelixDiscordRichPresenceSettingsChanged message)
    {
        UpdateDiscordPresence();
    }

    public void UpdateDiscordPresence()
    {
        if (!_windowVm.Cfg.GetCVar(CVars.HelixDiscordRichPresenceEnabled))
        {
            HelixDiscordRichPresence.Instance.Stop();
            return;
        }

        if (_windowVm.ConnectingVM != null)
        {
            HelixDiscordRichPresence.Instance.SetActivity("Launching a server");
            return;
        }

        if (_gameRunning)
        {
            var mode = HelixDiscordPresenceSettings.GetInGamePresenceMode(
                _windowVm.Cfg.GetCVar(CVars.HelixDiscordInGamePresenceMode));
            if (mode == HelixDiscordInGamePresenceMode.Helix)
            {
                HelixDiscordRichPresence.Instance.SetActivity(BuildGameActivity(HelixGameActivity.Presence));
                return;
            }

            HelixDiscordRichPresence.Instance.Stop();
            return;
        }

        if (!_windowVm.LoggedIn)
        {
            HelixDiscordRichPresence.Instance.SetActivity(
                string.IsNullOrWhiteSpace(_windowVm.BusyTask)
                    ? "At login screen"
                    : _windowVm.BusyTask);
            return;
        }

        if (_windowVm.Tabs.Count == 0)
        {
            HelixDiscordRichPresence.Instance.SetActivity("In launcher");
            return;
        }

        var selectedIndex = Math.Clamp(_windowVm.SelectedIndex, 0, _windowVm.Tabs.Count - 1);
        var state = selectedIndex switch
        {
            0 => "Viewing home",
            1 => "Browsing servers",
            2 => "Reading news",
            3 => "Managing resource packs",
            4 => "Managing keybind configs",
            5 => "Changing settings",
            _ => $"Viewing {_windowVm.Tabs[selectedIndex].Name}"
        };

        HelixDiscordRichPresence.Instance.SetActivity(state);
    }

    private static HelixDiscordActivity BuildGameActivity(HelixGamePresence? presence)
    {
        if (presence == null)
        {
            return new HelixDiscordActivity(
                Details: "Playing via BomBom Launcher",
                State: "Space Station 14");
        }

        var server = FirstNonBlank(presence.ServerName, presence.ServerAddress, "Space Station 14");
        var details = $"Playing via BomBom on {server}";

        var stateParts = new[]
        {
            presence.Username,
            FirstNonBlank(presence.Preset, presence.Map)
        }.Where(x => !string.IsNullOrWhiteSpace(x));

        var state = string.Join(" - ", stateParts);
        if (string.IsNullOrWhiteSpace(state))
            state = "Space Station 14";

        return new HelixDiscordActivity(
            Details: details,
            State: state,
            PlayerCount: presence.PlayerCount,
            MaxPlayers: presence.SoftMaxPlayerCount);
    }

    private static string? FirstNonBlank(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }
}
