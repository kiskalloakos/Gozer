using UnityEngine;

public static class PlayerProgression
{
    public const string ReinforcedMeleeKey = "Player.Upgrade.ReinforcedMelee";
    public const int ReinforcedMeleeCost = 8;
    public const int BaseMeleeDamage = 1;
    public const int ReinforcedMeleeDamage = 2;

    public enum PurchaseResult
    {
        Purchased,
        NotEnoughGold,
        AlreadyOwned
    }

    public static bool HasReinforcedMelee => (GameState.Active != null ? GameState.Active.meleeUpgrade : PlayerPrefs.GetInt(ReinforcedMeleeKey, 0)) == 1;
    public static int MeleeLevel => HasReinforcedMelee ? 2 : 1;

    public static int CurrentMeleeDamage => HasReinforcedMelee
        ? ReinforcedMeleeDamage
        : BaseMeleeDamage;

    public static PurchaseResult PurchaseReinforcedMelee(out int remainingGold)
    {
        GameState.InstallFromRuntime();
        remainingGold = TownHubController.GetGoldBalance();

        if (HasReinforcedMelee) return PurchaseResult.AlreadyOwned;
        if (!TownHubController.TrySpendGold(ReinforcedMeleeCost, out remainingGold))
            return PurchaseResult.NotEnoughGold;

        GameState.Active.meleeUpgrade = 1;
        return PurchaseResult.Purchased;
    }
}
