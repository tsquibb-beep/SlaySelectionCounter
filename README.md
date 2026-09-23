# Slay Selection Counter

A small quality-of-life mod for **Slay the Spire 2**. On any screen where you pick several
cards, it shows how many you have picked so far, e.g. **(7/10)**. The count goes down again
when you unselect a card, and it turns gold when your selection is full.

Written for the **Sealed Deck** modifier (choose 10 of 30 cards in Custom and Daily runs),
but it works on every multi-pick screen: removing, transforming, upgrading or enchanting
several cards, and in-combat "choose N cards" prompts. Single-pick screens are left alone.

The mod is purely visual. It does not change gameplay.

## Installation

Install with Vortex, or copy the `SlaySelectionCounter` folder into
`Slay the Spire 2/mods/` so you end up with `mods/SlaySelectionCounter/SlaySelectionCounter.dll`.

## Settings

Edit `SlaySelectionCounter.config.jsonc` in the mod folder, then restart the game. To keep
your settings through mod updates, copy the file to `%APPDATA%\SlayTheSpire2\`; that copy is
read in preference to the one in the mod folder.

| Setting          | Default     | What it does |
|------------------|-------------|--------------|
| `enabled`        | `true`      | Master switch. |
| `style`          | `"prompt"`  | `"prompt"` adds the count to the instructions at the bottom of the screen. `"badge"` shows a large counter above the Confirm button (experimental — its placement has had less testing). `"both"` shows both. |
| `badgeFontSize`  | `56`        | Size of the badge counter. |
| `badgeColor`     | `"#fff6e2"` | Badge colour while you are still picking. |
| `badgeFullColor` | `"#efc851"` | Colour once the selection is full (the prompt counter uses it too). |

## Building from source

Requires the .NET 9 SDK and a local install of the game.

```bash
./deploy.sh ["/path/to/Slay the Spire 2"]   # build and install into the game's mods folder
./package.sh ["/path/to/Slay the Spire 2"]  # build a release ZIP into dist/
```

## Licence

MIT. See [LICENSE](LICENSE).
