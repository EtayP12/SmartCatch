using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;

namespace SmartCatch;

/// <summary>
/// SmartCatch mod entry point. Skips the fishing minigame and simulates outcomes
/// based on fish difficulty, fishing level, rod, bait, tackle, and other factors.
/// </summary>
public class ModEntry : Mod
{
    private ModConfig _config = null!;

    public override void Entry(IModHelper helper)
    {
        _config = helper.ReadConfig<ModConfig>();
        Helper.Events.Display.MenuChanged += OnMenuChanged;
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (e.NewMenu is not BobberBar bar)
            return;

        var player = Game1.player;
        if (player.CurrentTool is not FishingRod rod)
            return;

        // Get values from BobberBar (game has already computed fish, size, base quality, etc.)
        string whichFish = bar.whichFish;
        int fishSize = bar.fishSize;
        int difficulty = (int)bar.difficulty;
        bool treasure = bar.treasure;
        bool fromFishPond = bar.fromFishPond;
        bool bossFish = bar.bossFish;
        string setFlagOnCatch = bar.setFlagOnCatch ?? "";
        int numCaught = bar.bossFish ? 1 : (bar.challengeBaitFishes > 0 ? bar.challengeBaitFishes : 1);
        int baseQualityFromGame = bar.fishQuality;

        // Calculate bar size and run smart catch logic
        int barSize = SmartCatchCalculator.CalculateBarSize(player, rod);
        int qualityBobberCount = SmartCatchCalculator.CountQualityBobbers(rod);
        bool isTrainingRod = SmartCatchCalculator.IsTrainingRod(rod);

        // Debug: log inputs
        var logLevel = _config.DebugLogging ? LogLevel.Info : LogLevel.Debug;
        if (_config.DebugLogging)
        {
            Monitor.Log($"[SmartCatch] INPUTS - Fish: {whichFish}, Size: {fishSize}, Difficulty: {difficulty}, " +
                $"Treasure: {treasure}, BossFish: {bossFish}, FromPond: {fromFishPond}, NumCaught: {numCaught}", logLevel);
            Monitor.Log($"[SmartCatch] INPUTS - FishingLevel: {player.FishingLevel}, BarSize: {barSize}, " +
                $"QualityBobbers: {qualityBobberCount}, IsTrainingRod: {isTrainingRod}, BaseQualityFromGame: {baseQualityFromGame}", logLevel);
            double successProb = SmartCatchCalculator.CalculateSuccessProbability(barSize, difficulty) * _config.SuccessChanceMultiplier;
            double perfectProb = SmartCatchCalculator.CalculatePerfectProbability(barSize, difficulty, treasure);
            double treasureProb = treasure ? SmartCatchCalculator.CalculateTreasureCaptureProbability(barSize, difficulty) : 0;
            Monitor.Log($"[SmartCatch] PROBS - Success: {successProb:P2}, Perfect: {perfectProb:P2}, TreasureCapture: {treasureProb:P2}", logLevel);
        }

        var result = SmartCatchCalculator.Calculate(
            barSize,
            difficulty,
            baseQualityFromGame,
            qualityBobberCount,
            isTrainingRod,
            treasure,
            _config.AlwaysSuccess,
            _config.AlwaysPerfect,
            _config.SuccessChanceMultiplier);

        // Debug: log outputs
        if (_config.DebugLogging)
        {
            Monitor.Log($"[SmartCatch] OUTPUTS - Success: {result.Success}, Quality: {result.FishQuality}, " +
                $"WasPerfect: {result.WasPerfect}, TreasureCaught: {result.TreasureCaught}", logLevel);
        }

        if (!result.Success)
        {
            if (_config.AllowMinigameOnFail)
            {
                if (_config.DebugLogging)
                    Monitor.Log($"[SmartCatch] Catch would fail - allowing minigame for player to try.", logLevel);
                return;
            }
            if (_config.DebugLogging)
                Monitor.Log($"[SmartCatch] Fish escaped!", logLevel);
            rod.doneFishing(player, consumeBaitAndTackle: true);
            Game1.exitActiveMenu();
            return;
        }

        // Apply quality override if configured
        int fishQuality = _config.QualityOverride >= 0
            ? Math.Clamp(_config.QualityOverride, 0, 4)
            : result.FishQuality;

        // Success - call pullFishFromWater and close menu
        // Pass treasure only if we captured it (fish caught but treasure lost is possible)
        bool treasureCaught = treasure && result.TreasureCaught;
        if (_config.DebugLogging)
            Monitor.Log($"[SmartCatch] FINAL - Fish: {whichFish}, Quality: {fishQuality}, Perfect: {result.WasPerfect}, TreasureGiven: {treasureCaught}", logLevel);
        rod.pullFishFromWater(
            whichFish,
            fishSize,
            fishQuality,
            difficulty,
            treasureCaught,
            result.WasPerfect,
            fromFishPond,
            setFlagOnCatch,
            bossFish,
            numCaught);

        Game1.exitActiveMenu();
    }
}
