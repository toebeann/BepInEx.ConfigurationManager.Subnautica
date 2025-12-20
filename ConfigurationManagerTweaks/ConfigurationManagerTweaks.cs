using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using System;
using UnityEngine;
using UWE;

namespace ConfigurationManager;

/// <summary>
/// A simple plugin to override the default hotkey of Configuration Manager and make the hotkey configurable in-game.
/// https://github.com/toebeann/BepInEx.ConfigurationManager.Subnautica
/// </summary>
[DisallowMultipleComponent]
[BepInDependency(ConfigurationManager.GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("Subnautica")]
[BepInProcess("SubnauticaZero")]
public sealed class ConfigurationManagerTweaks : BaseUnityPlugin
{
    //private const string freezeTimeIdName = "IngameMenu";

    private ConfigEntry<KeyboardShortcut> showConfigManager;
    private ConfigEntry<bool> pauseWhileOpen;

    internal static ConfigurationManagerTweaks Instance { get; private set; }

    private readonly Lazy<ConfigurationManager> configurationManager = new(() => Instance.GetComponent<ConfigurationManager>() ?? Chainloader.ManagerObject.GetComponent<ConfigurationManager>());
    private ConfigurationManager ConfigurationManager => configurationManager.Value;

    private void Awake()
    {
        // enforce singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }

        showConfigManager = Config.Bind(
            section: nameof(ConfigurationManager),
            key: "Show Configuration Manager",
            defaultValue: new KeyboardShortcut(KeyCode.F5),
            description: "Shortcut to open the Configuration Manager overlay."
        );

        pauseWhileOpen = Config.Bind(
            section: nameof(ConfigurationManager),
            key: "Pause while open",
            defaultValue: true,
            configDescription: new(
                description: "Whether the game should be paused while the Configuration Manager overlay is open.",
                tags: [new ConfigurationManagerAttributes { Order = -10 }]
            )
        );
    }

    private void OnEnable()
    {
        ConfigurationManager.OverrideHotkey = true;

        ConfigurationManager.DisplayingWindowChanged -= ConfigurationManager_DisplayingWindowChanged;
        ConfigurationManager.DisplayingWindowChanged += ConfigurationManager_DisplayingWindowChanged;

        pauseWhileOpen.SettingChanged -= PauseWhileOpen_SettingChanged;
        pauseWhileOpen.SettingChanged += PauseWhileOpen_SettingChanged;
    }

    private void PauseWhileOpen_SettingChanged(object sender, EventArgs e)
    {
        if (FindObjectOfType<Player>() == null) return;

        if (pauseWhileOpen.Value && ConfigurationManager.DisplayingWindow)
        {
            Pause(true);
        }
        else if (ConfigurationManager.DisplayingWindow)
        {
            Pause(false);
        }
    }

    private void ConfigurationManager_DisplayingWindowChanged(object _, ValueChangedEventArgs<bool> e)
    {
        if (FindObjectOfType<Player>() == null) return;

        if (e.NewValue)
        {
            UWE.Utils.PushLockCursor(false);
            if (pauseWhileOpen.Value)
            {
                Pause(true);
            }
        }
        else
        {
            UWE.Utils.PopLockCursor();
            if (pauseWhileOpen.Value)
            {
                Pause(false);
            }
        }
    }

    private void Update()
    {
        if (showConfigManager.Value.IsDown())
        {
            ConfigurationManager.DisplayingWindow = !ConfigurationManager.DisplayingWindow;
        }
    }

    private void OnDisable()
    {
        pauseWhileOpen.SettingChanged -= PauseWhileOpen_SettingChanged;
        ConfigurationManager.DisplayingWindowChanged -= ConfigurationManager_DisplayingWindowChanged;
        ConfigurationManager.OverrideHotkey = false;
    }

    private static void Pause(bool pause)
    {
        if (pause)
        {
            FreezeTime.Begin(FreezeTime.Id.IngameMenu);
        }
        else
        {
            FreezeTime.End(FreezeTime.Id.IngameMenu);
        }
    }

    private sealed class ConfigurationManagerAttributes { public int? Order; }
}
