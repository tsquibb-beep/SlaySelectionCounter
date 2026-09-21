using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace SlaySelectionCounter;

/// <summary>
/// Attaches a counter to every card-grid screen that can ask for more than one card. Each
/// one sets its prompt text in <c>_Ready</c>, so a postfix there sees the finished prompt.
/// </summary>
[HarmonyPatch]
internal static class SelectionScreenPatches
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(NSimpleCardSelectScreen), nameof(NSimpleCardSelectScreen._Ready));
        yield return AccessTools.Method(typeof(NDeckCardSelectScreen), nameof(NDeckCardSelectScreen._Ready));
        yield return AccessTools.Method(typeof(NDeckTransformSelectScreen), nameof(NDeckTransformSelectScreen._Ready));
        yield return AccessTools.Method(typeof(NDeckUpgradeSelectScreen), nameof(NDeckUpgradeSelectScreen._Ready));
        yield return AccessTools.Method(typeof(NDeckEnchantSelectScreen), nameof(NDeckEnchantSelectScreen._Ready));
        yield return AccessTools.Method(typeof(NCombatPileCardSelectScreen), nameof(NCombatPileCardSelectScreen._Ready));
    }

    private static void Postfix(Control __instance)
    {
        SelectionCounter.Attach(__instance);
    }
}
