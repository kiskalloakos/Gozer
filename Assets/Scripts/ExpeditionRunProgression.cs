using UnityEngine;

public static class ExpeditionRunProgression
{
    public const string CompletedRunsKey = "Expedition.CompletedProceduralRuns";
    public const string PendingThreatIncreaseKey = "Expedition.PendingThreatIncrease";

    const int MaximumRegularEnemyBonus = 24;
    const int MaximumExtractionReinforcementBonus = 6;

    public static int CompletedRuns => Mathf.Max(0, PlayerPrefs.GetInt(CompletedRunsKey, 0));
    public static int ThreatLevel => CompletedRuns + 1;
    public static int RegularEnemyBonus => Mathf.Min(CompletedRuns, MaximumRegularEnemyBonus);
    public static int ExtractionReinforcementBonus =>
        Mathf.Min(CompletedRuns / 2, MaximumExtractionReinforcementBonus);

    public static void RecordSuccessfulProceduralRun()
    {
        int completedRuns = CompletedRuns + 1;
        PlayerPrefs.SetInt(CompletedRunsKey, completedRuns);
        PlayerPrefs.SetInt(PendingThreatIncreaseKey, completedRuns + 1);
        PlayerPrefs.Save();
    }

    public static bool TryConsumePendingThreatIncrease(out int threatLevel)
    {
        threatLevel = PlayerPrefs.GetInt(PendingThreatIncreaseKey, 0);
        if (threatLevel <= 0) return false;
        PlayerPrefs.DeleteKey(PendingThreatIncreaseKey);
        PlayerPrefs.Save();
        return true;
    }
}
