using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UWE;

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
    public const string Version = "1.2";

    private const string freezeTimeIdName = "IngameMenu";

    private Lazy<ConfigurationManager> configurationManager =
        new(() => Instance.GetComponent<ConfigurationManager>() ?? Chainloader.ManagerObject.GetComponent<ConfigurationManager>());
    private ConfigurationManager ConfigurationManager => configurationManager.Value;

    private ConfigEntry<KeyboardShortcut> showConfigManager;
    private ConfigEntry<bool> pauseWhileOpen;

    internal static ConfigurationManagerTweaks Instance { get; private set; }

    private static Lazy<Type> player = new(() => AccessTools.TypeByName(nameof(Player)));
    private static Lazy<MethodInfo> freezeTimeBegin = new(() => AccessTools.Method($"{nameof(UWE)}.{nameof(FreezeTime)}:{nameof(FreezeTime.Begin)}"));
    private static Lazy<MethodInfo> freezeTimeEnd = new(() => AccessTools.Method($"{nameof(UWE)}.{nameof(FreezeTime)}:{nameof(FreezeTime.End)}"));
    private static Lazy<ParameterInfo> freezeTimeIdParameter = new(() => freezeTimeBegin.Value.GetParameters().First());
    private static Lazy<object> freezeTimeIdIngameMenuValue =
        new(() => AccessTools.Field(AccessTools.TypeByName("UWE.FreezeTime+Id"), freezeTimeIdName).GetValue(null));

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

        ConfigurationManager.RegisterCustomSettingDrawer(typeof(KeyCode), DrawKeyCode);

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
                tags: new[] { new ConfigurationManagerAttributes { Order = -10 } }
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

    private void PauseWhileOpen_SettingChanged(object _, EventArgs __)
    {
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
        ConfigurationManager.DisplayingWindowChanged -= ConfigurationManager_DisplayingWindowChanged;
        ConfigurationManager.OverrideHotkey = false;
    }

    private static void Pause(bool pause)
    {
        if (FindObjectOfType(player.Value) != null)
        {
            if (pause)
            {
                freezeTimeBegin.Value.Invoke(null, freezeTimeIdParameter.Value.ParameterType.FullName == typeof(string).FullName
                    ? new object[] { freezeTimeIdName, true }
                    : new object[] { freezeTimeIdIngameMenuValue.Value });
            }
            else
            {
                freezeTimeEnd.Value.Invoke(null, freezeTimeIdParameter.Value.ParameterType.FullName == typeof(string).FullName
                    ? new object[] { freezeTimeIdName }
                    : new object[] { freezeTimeIdIngameMenuValue.Value });
            }
        }
    }

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

    private class ConfigurationManagerAttributes { public int? Order; }
}

