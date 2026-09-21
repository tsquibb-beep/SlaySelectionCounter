using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace SlaySelectionCounter;

/// <summary>
/// Shows "picked / allowed" on one card-selection screen for as long as it is open.
///
/// Every multi-pick screen keeps its picks in a private <c>_selectedCards</c> set, but each
/// changes it by a different route (clicks, cancelling a preview clears it, the combat-pile
/// screen rebuilds it when the pile changes). Rather than chase every mutation, we compare the
/// count once per frame and only touch the UI when it moves.
/// </summary>
internal sealed class SelectionCounter
{
    /// <summary>Bottom-right fallback for screens without a Confirm button: offset from the corner.</summary>
    private static readonly Vector2 _cornerFallback = new(260f, 300f);

    private const float BadgeGap = 12f;

    private readonly Control _screen;
    private readonly FieldInfo _selectedField;
    private readonly int _max;
    private readonly MegaRichTextLabel? _prompt;
    private readonly string _promptText = "";
    private readonly Control? _confirmButton;
    private readonly Label? _badge;
    private readonly CounterConfig _config;
    private int _lastCount = -1;
    private bool _loggedTickFailure;

    private SelectionCounter(Control screen, FieldInfo selectedField, int max, CounterConfig config)
    {
        _screen = screen;
        _selectedField = selectedField;
        _max = max;
        _config = config;

        _prompt = ReadField<MegaRichTextLabel>(screen, "_infoLabel");
        if (_prompt != null && config.ShowPrompt)
        {
            _promptText = _prompt.Text ?? "";
        }

        _confirmButton = ReadField<Control>(screen, "_confirmButton");

        if (config.ShowBadge)
        {
            _badge = CreateBadge();
            screen.AddChild(_badge);
        }
    }

    /// <summary>Hooks a freshly readied selection screen. Does nothing for single-pick screens.</summary>
    public static void Attach(Control screen)
    {
        CounterConfig config = CounterConfig.Current;
        if (!config.ShowPrompt && !config.ShowBadge)
        {
            return;
        }

        try
        {
            FieldInfo? selectedField = AccessTools.Field(screen.GetType(), "_selectedCards");
            FieldInfo? prefsField = AccessTools.Field(screen.GetType(), "_prefs");
            if (selectedField == null || prefsField?.GetValue(screen) is not CardSelectorPrefs prefs)
            {
                Log.Warn($"[SlaySelectionCounter] {screen.GetType().Name} has no selection state we recognise; skipping.");
                return;
            }

            // The combat-pile screen caps the target at the number of cards actually on offer,
            // so "choose 3" from a 2-card pile completes at 2. Do the same, which also keeps
            // "any number" prompts honest.
            int max = prefs.MaxSelect;
            if (ReadField<IReadOnlyList<CardModel>>(screen, "_cards") is { } cards)
            {
                max = Math.Min(max, cards.Count);
            }

            if (max <= 1)
            {
                return;
            }

            var counter = new SelectionCounter(screen, selectedField, max, config);
            SceneTree tree = screen.GetTree();
            tree.ProcessFrame += counter.Tick;
            screen.TreeExiting += () => tree.ProcessFrame -= counter.Tick;
            counter.Tick();

            Log.Info($"[SlaySelectionCounter] Counting on {screen.GetType().Name} (max={max}, prompt={counter._prompt != null && config.ShowPrompt}, badge={counter._badge != null}).");
        }
        catch (Exception ex)
        {
            Log.Error($"[SlaySelectionCounter] Could not attach to {screen.GetType().Name}: {ex}");
        }
    }

    private void Tick()
    {
        try
        {
            if (!GodotObject.IsInstanceValid(_screen))
            {
                return;
            }

            int count = (_selectedField.GetValue(_screen) as ICollection)?.Count ?? 0;
            if (_badge != null)
            {
                // Cheap, and keeps the badge put if the window is resized mid-selection.
                PlaceBadge();
            }

            if (count == _lastCount)
            {
                return;
            }

            _lastCount = count;
            UpdatePrompt(count);
            UpdateBadge(count);
        }
        catch (Exception ex)
        {
            if (!_loggedTickFailure)
            {
                _loggedTickFailure = true;
                Log.Error($"[SlaySelectionCounter] Update failed on {_screen.GetType().Name}: {ex}");
            }
        }
    }

    private void UpdatePrompt(int count)
    {
        if (_prompt == null || !_config.ShowPrompt || !GodotObject.IsInstanceValid(_prompt))
        {
            return;
        }

        string counter = $"({count}/{_max})";
        if (_prompt.BbcodeEnabled && count >= _max)
        {
            counter = $"[color=#{_config.BadgeFullColor.ToHtml(false)}]{counter}[/color]";
        }

        // The enchant screen wraps its prompt in [center]...[/center]; keep the counter inside
        // the centring rather than dangling after it.
        const string closeCenter = "[/center]";
        _prompt.Text = _promptText.EndsWith(closeCenter, StringComparison.Ordinal)
            ? $"{_promptText[..^closeCenter.Length]} {counter}{closeCenter}"
            : $"{_promptText} {counter}";
    }

    private void UpdateBadge(int count)
    {
        if (_badge == null)
        {
            return;
        }

        _badge.Text = $"{count}/{_max}";
        _badge.AddThemeColorOverride("font_color", count >= _max ? _config.BadgeFullColor : _config.BadgeColor);
    }

    private Label CreateBadge()
    {
        var badge = new Label
        {
            Name = "SlaySelectionCounterBadge",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        // Borrow the game's own typeface from the prompt so the badge does not look bolted on.
        Font? font = _prompt?.GetThemeFont("normal_font");
        if (font != null)
        {
            badge.AddThemeFontOverride("font", font);
        }

        badge.AddThemeFontSizeOverride("font_size", _config.BadgeFontSize);
        badge.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.85f));
        badge.AddThemeConstantOverride("outline_size", Math.Max(4, _config.BadgeFontSize / 7));
        badge.Size = new Vector2(240f, _config.BadgeFontSize * 1.4f);
        return badge;
    }

    /// <summary>
    /// Sits the badge just above where the Confirm button comes to rest. The button slides
    /// off-screen while disabled, so we use its resting position (<c>_showPos</c>) rather than
    /// its current one — otherwise the badge would ride in and out with it.
    /// </summary>
    private void PlaceBadge()
    {
        if (_badge == null)
        {
            return;
        }

        Vector2 buttonTopCentre;
        if (_confirmButton is NConfirmButton button
            && GodotObject.IsInstanceValid(button)
            && button.GetParent() is CanvasItem buttonParent
            && ReadField<object>(button, "_showPos") is Vector2 showPos)
        {
            Vector2 local = showPos + new Vector2(button.Size.X / 2f, 0f);
            buttonTopCentre = buttonParent.GetGlobalTransform() * local;
        }
        else
        {
            buttonTopCentre = _screen.GetGlobalRect().End - _cornerFallback;
        }

        Vector2 inScreen = _screen.GetGlobalTransform().AffineInverse() * buttonTopCentre;
        _badge.Position = inScreen - new Vector2(_badge.Size.X / 2f, _badge.Size.Y + BadgeGap);
    }

    private static T? ReadField<T>(object instance, string name) where T : class
    {
        return AccessTools.Field(instance.GetType(), name)?.GetValue(instance) as T;
    }
}
