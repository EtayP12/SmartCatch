using StardewValley;
using StardewValley.Tools;

namespace SmartCatch;

/// <summary>
/// Calculates bar size, success probability, quality, and perfect-catch probability
/// based on fishing level, rod, bait, tackle, and fish difficulty.
/// </summary>
public static class SmartCatchCalculator
{
    private const int CorkBobberId = 692;
    private const int QualityBobberId = 877;
    private const int DeluxeBaitId = 908;
    private const int TrainingRodId = 336;

    /// <summary>
    /// Result of the smart catch calculation.
    /// </summary>
    public record Result(
        bool Success,
        int FishQuality,
        bool WasPerfect,
        bool TreasureCaught
    );

    /// <summary>
    /// Calculates the fishing bar size in pixels based on player and equipment.
    /// </summary>
    public static int CalculateBarSize(Farmer player, FishingRod rod)
    {
        if (rod == null || player == null)
            return 96;

        int fishingLevel = player.FishingLevel;
        bool isTrainingRod = rod.QualifiedItemId == "(O)336" || rod.ParentSheetIndex == TrainingRodId;

        // Training Rod: use effective level 5 for bar size when below level 5
        if (isTrainingRod && fishingLevel < 5)
            fishingLevel = 5;

        int barSize = 96 + (fishingLevel * 8);

        // Cork Bobber: +24px each; Deluxe Bait: +12px
        if (rod.attachments?.Length > 0)
        {
            foreach (var attachment in rod.attachments)
            {
                if (attachment == null) continue;
                if (attachment.QualifiedItemId == "(O)692" || attachment.ParentSheetIndex == CorkBobberId)
                    barSize += 24;
                else if (attachment.QualifiedItemId == "(O)908" || attachment.ParentSheetIndex == DeluxeBaitId)
                    barSize += 12;
            }
        }

        // Master enchant: +8px (check via GetEnchantmentLevel or similar)
        var enchantments = rod.enchantments;
        if (enchantments != null)
        {
            foreach (var enc in enchantments)
            {
                if (enc != null && enc.GetType().Name.Contains("Master"))
                {
                    barSize += 8;
                    break;
                }
            }
        }

        return barSize;
    }

    /// <summary>
    /// Counts how many Quality Bobbers are equipped.
    /// </summary>
    public static int CountQualityBobbers(FishingRod rod)
    {
        if (rod?.attachments == null) return 0;
        int count = 0;
        foreach (var attachment in rod.attachments)
        {
            if (attachment != null && (attachment.QualifiedItemId == "(O)877" || attachment.ParentSheetIndex == QualityBobberId))
                count++;
        }
        return count;
    }

    /// <summary>
    /// Checks if the player is using the Training Rod.
    /// </summary>
    public static bool IsTrainingRod(FishingRod rod)
    {
        return rod != null && (rod.QualifiedItemId == "(O)336" || rod.ParentSheetIndex == TrainingRodId);
    }

    // Difficulty 15-100; cap ensures high-difficulty fish never hit 100% even at max bar
    private const int MinDifficulty = 15;
    private const int MaxDifficulty = 100;

    /// <summary>
    /// Logarithmic difficulty factor (0-1). Uses squared curve so mid-difficulty fish
    /// (e.g. 40) keep high caps; only high difficulty (80+) drops significantly.
    /// </summary>
    private static double LogarithmicDifficultyFactor(int difficulty)
    {
        if (difficulty <= MinDifficulty) return 0;
        double t = (double)(difficulty - MinDifficulty) / (MaxDifficulty - MinDifficulty);
        return t * t;
    }

    /// <summary>
    /// Maximum success probability for a given difficulty. Uses logarithmic scaling
    /// so caps stay high for mid-difficulty fish (e.g. diff 40 ~98% vs linear ~94%).
    /// </summary>
    private static double MaxSuccessProbabilityForDifficulty(int difficulty)
    {
        if (difficulty <= MinDifficulty) return 0.99;
        double t = LogarithmicDifficultyFactor(difficulty);
        return 0.99 - t * 0.16;
    }

    /// <summary>
    /// Maximum perfect-catch probability for a given difficulty.
    /// 99% at diff 15, stays above 90% at diff 40, drops to ~22% at diff 100.
    /// </summary>
    private static double MaxPerfectProbabilityForDifficulty(int difficulty)
    {
        if (difficulty <= MinDifficulty) return 0.99;
        double t = (double)(difficulty - MinDifficulty) / (MaxDifficulty - MinDifficulty);
        return 0.99 - t * t * 0.77;
    }

    /// <summary>
    /// Maximum treasure-capture probability for a given difficulty.
    /// </summary>
    private static double MaxTreasureProbabilityForDifficulty(int difficulty)
    {
        if (difficulty <= MinDifficulty) return 0.95;
        double t = LogarithmicDifficultyFactor(difficulty);
        return 0.95 - t * 0.22;
    }

    /// <summary>
    /// Calculates success probability (0-1) based on bar size vs fish difficulty.
    /// Uses logarithmic (squared) difficulty for caps so mid-difficulty fish have higher success.
    /// </summary>
    public static double CalculateSuccessProbability(int barSize, int difficulty)
    {
        if (difficulty <= 0) return 1;
        double raw = (double)barSize / (difficulty * 2.5);
        double cap = MaxSuccessProbabilityForDifficulty(difficulty);
        return Math.Min(cap, Math.Min(1, raw));
    }

    private const int Level10BarSize = 176;

    /// <summary>
    /// Calculates perfect-catch probability (0-1) based on bar size vs fish difficulty.
    /// Level 10 bar (176) gets full cap. Smaller bars use (bar/176)^power where power
    /// increases with difficulty, so level 0 gives ~20% at diff 40 and ~1% at diff 100.
    /// </summary>
    public static double CalculatePerfectProbability(int barSize, int difficulty, bool hasTreasure)
    {
        if (difficulty <= 0) return 1;
        double cap = MaxPerfectProbabilityForDifficulty(difficulty);
        double barFactor;
        if (barSize >= Level10BarSize)
        {
            barFactor = 1.0;
        }
        else
        {
            double t = (double)(difficulty - MinDifficulty) / (MaxDifficulty - MinDifficulty);
            double power = 2.0 + 1.1 * t + 2.0 * t * t;
            barFactor = Math.Pow((double)barSize / Level10BarSize, power);
        }
        double prob = barFactor * cap;
        if (hasTreasure)
            prob *= 0.65;
        return prob;
    }

    /// <summary>
    /// Calculates treasure-capture probability (0-1) when a treasure chest appears.
    /// Uses logarithmic (squared) difficulty for caps.
    /// </summary>
    public static double CalculateTreasureCaptureProbability(int barSize, int difficulty)
    {
        if (difficulty <= 0) return 1;
        double raw = (double)barSize / (difficulty * 3.0);
        double cap = MaxTreasureProbabilityForDifficulty(difficulty);
        return Math.Min(cap, Math.Min(1, raw));
    }

    /// <summary>
    /// Calculates fish quality (0=normal, 1=silver, 2=gold, 4=iridium) from bar/difficulty ratio.
    /// </summary>
    public static int CalculateQuality(int barSize, int difficulty, int qualityBobberCount, bool wasPerfect, int baseQualityFromGame, bool isTrainingRod)
    {
        if (isTrainingRod)
            return 0;

        // Base quality from barSize/difficulty ratio
        double ratio = difficulty > 0 ? (double)barSize / difficulty : 2;
        int quality = ratio >= 2 ? 2 : (ratio >= 1.33 ? 1 : 0);

        // Use game's base quality if it's higher (game uses distance/skill for quality)
        quality = Math.Max(quality, baseQualityFromGame);

        // Quality Bobber: +1 per bobber
        quality += qualityBobberCount;
        if (quality > 2 && quality < 4) quality = 4;

        // Perfect catch: +1 if silver or gold
        if (wasPerfect && quality >= 1 && quality < 4)
            quality = Math.Min(4, quality + 1);

        return Math.Clamp(quality, 0, 4);
    }

    /// <summary>
    /// Performs the full smart catch calculation.
    /// </summary>
    /// <param name="barSize">Calculated bar size in pixels.</param>
    /// <param name="difficulty">Fish difficulty (15-100).</param>
    /// <param name="baseQualityFromGame">Quality from BobberBar (game-computed).</param>
    /// <param name="qualityBobberCount">Number of Quality Bobbers equipped.</param>
    /// <param name="isTrainingRod">Whether the Training Rod is used.</param>
    /// <param name="hasTreasure">Whether a treasure chest appeared (reduces perfect chance).</param>
    /// <param name="alwaysSuccess">If true, never fail the catch.</param>
    /// <param name="alwaysPerfect">If true, always treat as perfect.</param>
    /// <param name="successChanceMultiplier">Scale factor for success probability.</param>
    public static Result Calculate(
        int barSize,
        int difficulty,
        int baseQualityFromGame,
        int qualityBobberCount,
        bool isTrainingRod,
        bool hasTreasure,
        bool alwaysSuccess,
        bool alwaysPerfect,
        double successChanceMultiplier = 1.0)
    {
        double successProb = CalculateSuccessProbability(barSize, difficulty) * successChanceMultiplier;
        double perfectProb = CalculatePerfectProbability(barSize, difficulty, hasTreasure);

        bool success = alwaysSuccess || Random.Shared.NextDouble() < successProb;
        bool wasPerfect = alwaysPerfect || (success && Random.Shared.NextDouble() < perfectProb);

        // When treasure is present, roll separately for whether we capture it (fish caught but treasure lost)
        bool treasureCaught = false;
        if (success && hasTreasure)
        {
            double treasureProb = CalculateTreasureCaptureProbability(barSize, difficulty);
            treasureCaught = Random.Shared.NextDouble() < treasureProb;
        }

        int quality = CalculateQuality(barSize, difficulty, qualityBobberCount, wasPerfect, baseQualityFromGame, isTrainingRod);

        return new Result(success, quality, wasPerfect, treasureCaught);
    }
}
