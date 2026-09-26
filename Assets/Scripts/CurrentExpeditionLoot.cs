using UnityEngine;

/// <summary>Gold collected during a run, at risk until extraction.</summary>
public static class CurrentExpeditionLoot
{
    public static int Total
    {
        get
        {
            GameState.InstallFromRuntime();
            return GameState.Active.unsecuredGold;
        }
    }

    public static bool HasAny => Total > 0;

    public static bool Add(int amount)
    {
        if (amount <= 0) return false;
        GameState.InstallFromRuntime();
        if ((long)GameState.Active.gold + GameState.Active.unsecuredGold + amount > int.MaxValue)
            return false;
        GameState.Active.unsecuredGold += amount;
        return true;
    }

    public static int Secure()
    {
        GameState.InstallFromRuntime();
        int total = GameState.Active.unsecuredGold;
        if (total <= 0) return 0;
        if (GameState.Active.gold > int.MaxValue - total) return 0;
        GameState.Active.unsecuredGold = 0;
        GameState.Active.gold += total;
        GameState.Active.pendingSecuredGold += total;
        PlayerPrefs.SetInt(TownHubController.GoldKey, GameState.Active.gold);
        PlayerPrefs.SetInt("Expedition.UnsecuredGold", 0);
        PlayerPrefs.Save();
        return total;
    }

    public static void Lose()
    {
        GameState.InstallFromRuntime();
        GameState.Active.unsecuredGold = 0;
        PlayerPrefs.SetInt("Expedition.UnsecuredGold", 0);
    }

    /// <summary>Import the old parallel loot array from earlier save files.</summary>
    public static void MigrateLegacyState(GameState state)
    {
        if (state == null || state.expeditionLoot == null) return;
        long legacyTotal = 0;
        foreach (int amount in state.expeditionLoot) legacyTotal += Mathf.Max(0, amount);
        state.unsecuredGold = (int)System.Math.Min(int.MaxValue,
            System.Math.Max(state.unsecuredGold, legacyTotal));
        state.expeditionLoot = null;
    }

    public static void ResetSavedState()
    {
        if (GameState.Active != null)
        {
            GameState.Active.unsecuredGold = 0;
            GameState.Active.expeditionLoot = null;
        }
        PlayerPrefs.DeleteKey("Expedition.UnsecuredGold");
        PlayerPrefs.DeleteKey("Expedition.CurrentLoot.v1");
    }
}
