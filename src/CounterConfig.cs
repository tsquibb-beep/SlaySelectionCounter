using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace SlaySelectionCounter;

/// <summary>
/// Settings, read once at startup from SlaySelectionCounter.config.jsonc. Missing or malformed
/// files fall back to these defaults, so the mod never fails to load because of a bad config.
/// </summary>
internal sealed class CounterConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// "prompt" appends "(7/10)" to the instruction text at the bottom of the screen.
    /// "badge" shows a large separate counter above where the Confirm button appears.
    /// "both" shows both.
    /// </summary>
    [JsonPropertyName("style")]
    public string Style { get; set; } = "prompt";

    /// <summary>Font size of the badge counter. Ignored by the prompt style.</summary>
    [JsonPropertyName("badgeFontSize")]
    public int BadgeFontSize { get; set; } = 56;

    /// <summary>Badge colour while you still have cards left to pick.</summary>
    [JsonPropertyName("badgeColor")]
    public string BadgeColorHex { get; set; } = "#fff6e2";

    /// <summary>Badge colour once the selection is full.</summary>
    [JsonPropertyName("badgeFullColor")]
    public string BadgeFullColorHex { get; set; } = "#efc851";

    private static CounterConfig? _current;

    public static CounterConfig Current => _current ??= Load();

    public bool ShowPrompt => Enabled && (IsStyle("prompt") || IsStyle("both"));

    public bool ShowBadge => Enabled && (IsStyle("badge") || IsStyle("both"));

    public Color BadgeColor => ParseColor(BadgeColorHex, "#fff6e2");

    public Color BadgeFullColor => ParseColor(BadgeFullColorHex, "#efc851");

    private bool IsStyle(string style) => string.Equals(Style?.Trim(), style, StringComparison.OrdinalIgnoreCase);

    private static Color ParseColor(string value, string fallback)
    {
        try
        {
            return new Color(value);
        }
        catch (Exception)
        {
            Log.Warn($"[SlaySelectionCounter] '{value}' is not a valid colour, falling back to {fallback}.");
            return new Color(fallback);
        }
    }

    private const string FileName = "SlaySelectionCounter.config.jsonc";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Looks for the config next to the game's save data first, then beside the mod DLL.
    ///
    /// The save-data copy wins so that settings survive a Vortex update, which replaces the
    /// whole mod folder. The file is .jsonc rather than .json deliberately: the game scans
    /// mods/ recursively for *.json and tries to parse every one as a mod manifest.
    /// </summary>
    private static CounterConfig Load()
    {
        foreach (string path in CandidatePaths())
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                CounterConfig? loaded = JsonSerializer.Deserialize<CounterConfig>(File.ReadAllText(path), _jsonOptions);
                if (loaded != null)
                {
                    Log.Info($"[SlaySelectionCounter] Loaded config from {path}.");
                    return loaded;
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"[SlaySelectionCounter] Could not read {path} ({ex.Message}), trying the next location.");
            }
        }

        Log.Info("[SlaySelectionCounter] No config file found, using defaults.");
        return new CounterConfig();
    }

    private static IEnumerable<string> CandidatePaths()
    {
        string? userDir = null;
        try
        {
            userDir = OS.GetUserDataDir();
        }
        catch (Exception)
        {
            // Not fatal: fall back to the mod folder.
        }

        if (!string.IsNullOrEmpty(userDir))
        {
            yield return Path.Combine(userDir, FileName);
        }

        string? modDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (!string.IsNullOrEmpty(modDir))
        {
            yield return Path.Combine(modDir, FileName);
        }
    }
}
