using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System.Linq;
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
    public const string Version = "1.1";

    private ConfigurationManager configurationManager;
    private ConfigurationManager ConfigurationManager =>
        configurationManager ??= GetComponent<ConfigurationManager>() ?? Chainloader.ManagerObject.GetComponent<ConfigurationManager>();

    private ConfigEntry<KeyboardShortcut> showConfigManager;

    private void Awake()
    {
        ConfigurationManager.RegisterCustomSettingDrawer(typeof(KeyCode), DrawKeyCode);

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

    private static void DrawKeyCode(SettingEntryBase setting)
    {
        var _currentKeyboardShortcutToSet = Traverse.Create<SettingFieldDrawer>().Field<SettingEntryBase>("_currentKeyboardShortcutToSet");

        if (_currentKeyboardShortcutToSet.Value == setting)
        {
            GUILayout.Label("Press any key", GUILayout.ExpandWidth(true));
            GUIUtility.keyboardControl = -1;

            var input = UnityInput.Current;
            var keysToCheck = input.SupportedKeyCodes.Except(new[] { KeyCode.Mouse0, KeyCode.None }).ToArray();
            foreach (var key in keysToCheck)
            {
                if (input.GetKeyDown(key))
                {
                    setting.Set(key);
                    _currentKeyboardShortcutToSet.Value = null;
                    break;
                }
            }

            if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(false)))
                _currentKeyboardShortcutToSet.Value = null;
        }
        else
        {
            if (GUILayout.Button(setting.Get().ToString(), GUILayout.ExpandWidth(true)))
                _currentKeyboardShortcutToSet.Value = setting;

            if (GUILayout.Button("Clear", GUILayout.ExpandWidth(false)))
            {
                setting.Set(KeyCode.None);
                _currentKeyboardShortcutToSet.Value = null;
            }
        }
    }
}

