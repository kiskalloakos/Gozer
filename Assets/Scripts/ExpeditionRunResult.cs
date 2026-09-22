using UnityEngine;

public struct ExpeditionRunSummary
{
    public bool succeeded;
    public int securedGold;
    public int lostGold;
    public int healthUnits;
    public int nextThreat;
}

public static class ExpeditionRunResult
{
    const string PendingKey = "Expedition.PendingRunResult";
    const string SuccessKey = "Expedition.ResultSuccess";
    const string SecuredKey = "Expedition.ResultSecuredGold";
    const string LostKey = "Expedition.ResultLostGold";
    const string HealthKey = "Expedition.ResultHealthUnits";
    const string ThreatKey = "Expedition.ResultNextThreat";

    public static void RecordSuccess(int securedGold, int healthUnits)
    {
        Write(true, securedGold, 0, healthUnits,
            PlayerProgression.MeleeLevel >= 2 ? ExpeditionRunProgression.ThreatLevel : 0);
    }

    public static void RecordDefeat(int lostGold, int healthUnits)
        => Write(false, 0, lostGold, healthUnits, 0);

    static void Write(bool success, int secured, int lost, int health, int threat)
    {
        PlayerPrefs.SetInt(PendingKey, 1);
        PlayerPrefs.SetInt(SuccessKey, success ? 1 : 0);
        PlayerPrefs.SetInt(SecuredKey, Mathf.Max(0, secured));
        PlayerPrefs.SetInt(LostKey, Mathf.Max(0, lost));
        PlayerPrefs.SetInt(HealthKey, Mathf.Max(0, health));
        PlayerPrefs.SetInt(ThreatKey, Mathf.Max(0, threat));
        PlayerPrefs.Save();
    }

    public static bool TryConsume(out ExpeditionRunSummary result)
    {
        result = default;
        if (PlayerPrefs.GetInt(PendingKey, 0) == 0) return false;
        result.succeeded = PlayerPrefs.GetInt(SuccessKey, 0) == 1;
        result.securedGold = PlayerPrefs.GetInt(SecuredKey, 0);
        result.lostGold = PlayerPrefs.GetInt(LostKey, 0);
        result.healthUnits = PlayerPrefs.GetInt(HealthKey, 0);
        result.nextThreat = PlayerPrefs.GetInt(ThreatKey, 0);
        PlayerPrefs.DeleteKey(PendingKey);
        PlayerPrefs.DeleteKey(SuccessKey);
        PlayerPrefs.DeleteKey(SecuredKey);
        PlayerPrefs.DeleteKey(LostKey);
        PlayerPrefs.DeleteKey(HealthKey);
        PlayerPrefs.DeleteKey(ThreatKey);
        PlayerPrefs.Save();
        return true;
    }
}
