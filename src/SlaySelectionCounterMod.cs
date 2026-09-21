using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace SlaySelectionCounter;

/// <summary>
/// Entry point. The loader finds this via <see cref="ModInitializerAttribute"/> and calls
/// <see cref="Init"/> once at startup.
/// </summary>
[ModInitializer(nameof(Init))]
internal static class SlaySelectionCounterMod
{
    private const string HarmonyId = "tomasapan.SlaySelectionCounter";

    private static void Init()
    {
        try
        {
            new Harmony(HarmonyId).PatchAll(Assembly.GetExecutingAssembly());

            CounterConfig config = CounterConfig.Current;
            Log.Info($"[SlaySelectionCounter] Initialised (enabled={config.Enabled}, style={config.Style}).");
        }
        catch (Exception ex)
        {
            Log.Error($"[SlaySelectionCounter] Failed to initialise: {ex}");
        }
    }
}
