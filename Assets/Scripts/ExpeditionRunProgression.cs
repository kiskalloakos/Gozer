using UnityEngine;

public static class ExpeditionRunProgression
{
    public const string CompletedRunsKey = "Expedition.CompletedProceduralRuns";
    public const string PendingThreatIncreaseKey = "Expedition.PendingThreatIncrease";

    const int MaximumRegularEnemyBonus = 24;
    const int MaximumExtractionReinforcementBonus = 6;

    public static int CompletedRuns => Mathf.Max(0, GameState.Active != null ? GameState.Active.completedRuns : PlayerPrefs.GetInt(CompletedRunsKey, 0));
    public static int ThreatLevel => CompletedRuns + 1;
    public static bool HasCompletedTutorial => CompletedRuns > 0;
    public static int RegularEnemyBonus => Mathf.Min(CompletedRuns, MaximumRegularEnemyBonus);
    public static int ExtractionReinforcementBonus =>
        Mathf.Min(CompletedRuns / 2, MaximumExtractionReinforcementBonus);

    public static void RecoverCompletedTutorial(GameState state)
    {
        if (state == null || state.completedRuns != 0 || !state.resultSucceeded
            || string.IsNullOrEmpty(state.expeditionRunIdentity)) return;
        try
        {
            var identity = JsonUtility.FromJson<ExpeditionRunIdentity>(state.expeditionRunIdentity);
            if (identity.source != ExpeditionSeedSource.FixedField.ToString()) return;
            state.completedRuns = 1;
            if (state.pendingRunResult)
            {
                state.pendingThreat = 2;
                state.resultNextThreat = 2;
            }
        }
        catch (System.Exception) { /* An older or malformed identity cannot establish a completed tutorial. */ }
    }

    public static void RecordSuccessfulRun()
    {
        GameState.InstallFromRuntime();
        int completedRuns = CompletedRuns + 1;
        GameState.Active.completedRuns = completedRuns;
        GameState.Active.pendingThreat = completedRuns + 1;
    }

    public static bool TryConsumePendingThreatIncrease(out int threatLevel)
    {
        GameState.InstallFromRuntime();
        threatLevel = GameState.Active != null ? GameState.Active.pendingThreat : PlayerPrefs.GetInt(PendingThreatIncreaseKey, 0);
        if (threatLevel <= 0) return false;
        if (GameState.Active != null) GameState.Active.pendingThreat = 0;
        return true;
    }
}
