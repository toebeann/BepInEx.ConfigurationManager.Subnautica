using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using UnityEngine;

namespace ConfigurationManager;

/// <summary>
/// A simple plugin to override the default hotkey of Configuration Manager and make the hotkey configurable in-game.
/// https://github.com/toebeann/BepInEx.ConfigurationManager.Subnautica
/// </summary>
[DisallowMultipleComponent]
[BepInDependency(ConfigurationManager.GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInPlugin(GUID, "Configuration Manager Tweaks", Version)]
[BepInProcess("Subnautica")]
[BepInProcess("SubnauticaZero")]
public class ConfigurationManagerTweaks : BaseUnityPlugin
{
    /// <summary>
    /// GUID of this plugin
    /// </summary>
    public const string GUID = "Tobey.BepInEx.ConfigurationManagerTweaks.Subnautica";

    /// <summary>
    /// Version constant
    /// </summary>
    public const string Version = "1.0";

    private ConfigurationManager configurationManager;
    private ConfigurationManager ConfigurationManager =>
        configurationManager ??= GetComponent<ConfigurationManager>() ?? Chainloader.ManagerObject.GetComponent<ConfigurationManager>();

    private ConfigEntry<KeyboardShortcut> showConfigManager;

    private void Awake()
    {
        showConfigManager = Config.Bind(
            section: nameof(ConfigurationManager),
            key: "Show Configuration Manager",
            defaultValue: new KeyboardShortcut(KeyCode.F5),
            description: "Shortcut to open the Configuration Manager overlay."
        );
    }

    private void OnEnable() => ConfigurationManager.OverrideHotkey = true;

    private void Update()
    {
        if (showConfigManager.Value.IsDown())
        {
            ConfigurationManager.DisplayingWindow = !ConfigurationManager.DisplayingWindow;
        }
    }

    private void OnDisable() => ConfigurationManager.OverrideHotkey = false;
}

