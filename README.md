# SmartCatch

A [Stardew Valley](https://www.stardewvalley.net/) SMAPI mod that skips the fishing minigame and simulates outcomes based on fish difficulty, fishing level, rod, bait, tackle, and other factors.

## Features

- **Skip the minigame** — No more bobber bar; outcomes are rolled instantly
- **Realistic probabilities** — Success, perfect catch, and treasure chances scale with your gear and fish difficulty
- **Configurable** — Toggle always-success, always-perfect, minigame-on-fail, and more
- **Quality & XP** — Fish quality and XP bonuses are calculated as in the base game

## Installation

1. Install [SMAPI](https://smapi.io/)
2. [Download the latest release](https://github.com/EtayP12/SmartCatch/releases)
3. Extract into your Stardew Valley `Mods` folder

## Configuration

Edit `config/SmartCatch.SmartCatch.json` in your game folder:

| Option | Default | Description |
|--------|---------|-------------|
| `AlwaysSuccess` | `false` | Never fail a catch |
| `AlwaysPerfect` | `false` | Always get perfect catch bonus |
| `SuccessChanceMultiplier` | `1.0` | Scale factor for success probability |
| `QualityOverride` | `-1` | Force quality: 0=normal, 1=silver, 2=gold, 4=iridium |
| `AllowMinigameOnFail` | `true` | When a catch would fail, start the minigame so you can try |
| `DebugLogging` | `false` | Log each catch to the SMAPI console |

## Probability Chart

See how success, perfect, and treasure chances change with fishing level and equipment:

**[→ Interactive Probability Chart](https://etayp12.github.io/SmartCatch/)**

## How It Works

- **Bar size** — Base 96px + 8px per fishing level. Training Rod uses effective level 5 when below 5. Cork Bobber +24px, Deluxe Bait +12px, Master enchant +8px.
- **Success** — `min(cap, barSize / (difficulty × 2.5))` with a squared difficulty cap.
- **Perfect** — Bar factor × cap; bar factor uses `(barSize/176)^power` where power increases with difficulty (≈30% at diff 15, ≈20% at 40, ≈1% at 100 for level 0).
- **Treasure** — Separate roll; reduces perfect chance by 35% when present.

## Requirements

- Stardew Valley 1.6+
- SMAPI 4.0+
- .NET 6.0

## Building

```bash
dotnet build SmartCatch/SmartCatch.csproj
```

Output: `SmartCatch/bin/Debug/net6.0/SmartCatch.dll`

## License

[Choose your license]
