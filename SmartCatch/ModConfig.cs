namespace SmartCatch;

/// <summary>
/// User-configurable options for SmartCatch.
/// </summary>
public class ModConfig
{
    /// <summary>
    /// If true, always treat the catch as successful (never fail).
    /// </summary>
    public bool AlwaysSuccess { get; set; } = false;

    /// <summary>
    /// If true, always treat the catch as perfect (bonus XP and quality upgrade).
    /// </summary>
    public bool AlwaysPerfect { get; set; } = false;

    /// <summary>
    /// Scale factor for the calculated success probability (e.g. 1.5 = 50% higher chance).
    /// </summary>
    public double SuccessChanceMultiplier { get; set; } = 1.0;

    /// <summary>
    /// Force a quality tier: 0=normal, 1=silver, 2=gold, 4=iridium. Use -1 for auto (calculated).
    /// </summary>
    public int QualityOverride { get; set; } = -1;

    /// <summary>
    /// If true (default), when a catch would fail, the minigame starts and lets the player try to catch the fish.
    /// If false, failed catches immediately count as fish escaped.
    /// </summary>
    public bool AllowMinigameOnFail { get; set; } = true;

    /// <summary>
    /// If true, log inputs and outputs of each catch to the SMAPI console.
    /// </summary>
    public bool DebugLogging { get; set; } = false;
}
