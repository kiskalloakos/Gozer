using UnityEngine;

public struct ExpeditionRunSummary
{
    public bool succeeded;
    public int securedGold;
    public int lostGold;
    public int lostItemCount;
    public int healthUnits;
    public int nextThreat;
}

public static class ExpeditionRunResult
{
    const string PendingKey = "Expedition.PendingRunResult";
    const string SuccessKey = "Expedition.ResultSuccess";
    const string SecuredKey = "Expedition.ResultSecuredGold";
    const string LostKey = "Expedition.ResultLostGold";
    const string LostItemCountKey = "Expedition.ResultLostItemCount";
    const string HealthKey = "Expedition.ResultHealthUnits";
    const string ThreatKey = "Expedition.ResultNextThreat";

    public static void RecordSuccess(int securedGold, int healthUnits)
    {
        Write(true, securedGold, 0, healthUnits, 0,
            ExpeditionRunProgression.ThreatLevel);
    }

    public static void RecordDefeat(int lostGold, int healthUnits, int lostItemCount)
        => Write(false, 0, lostGold, healthUnits, lostItemCount, 0);

    static void Write(bool success, int secured, int lost, int health, int lostItemCount, int threat)
    {
        GameState.InstallFromRuntime();
        GameState.Active.pendingRunResult = true;
        GameState.Active.resultSucceeded = success;
        GameState.Active.resultSecuredGold = Mathf.Max(0, secured);
        GameState.Active.resultLostGold = Mathf.Max(0, lost);
        GameState.Active.resultLostItemCount = Mathf.Max(0, lostItemCount);
        GameState.Active.resultHealthUnits = Mathf.Max(0, health);
        GameState.Active.resultNextThreat = Mathf.Max(0, threat);
    }

    public static bool TryConsume(out ExpeditionRunSummary result)
    {
        result = default;
        GameState.InstallFromRuntime();
        if (!GameState.Active.pendingRunResult) return false;
        result.succeeded = GameState.Active.resultSucceeded;
        result.securedGold = GameState.Active.resultSecuredGold;
        result.lostGold = GameState.Active.resultLostGold;
        result.lostItemCount = GameState.Active.resultLostItemCount;
        result.healthUnits = GameState.Active.resultHealthUnits;
        result.nextThreat = GameState.Active.resultNextThreat;
        GameState.Active.pendingRunResult = false;
        return true;
    }
}
