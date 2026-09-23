using System;
using UnityEngine;

/// <summary>
/// In-memory save state. Gameplay systems read and mutate this object; the
/// session flow is the only system that serializes it to disk.
/// </summary>
[Serializable]
public sealed class GameState
{
    public static GameState Active { get; private set; }

    public static void InstallFromRuntime() => Active ??= FromLegacyPrefs();
    public static void Replace(GameState state) => Active = state ?? FromLegacyPrefs();

    public int health = ExpeditionPlayerHealth.DefaultMaxHealthUnits;
    public int gold;
    public int pendingSecuredGold;
    public float energy = ExpeditionPlayerEnergy.DefaultStartingEnergy;
    public bool injured;
    public int completedRuns;
    public int pendingThreat;
    public int meleeUpgrade;
    public int infirmaryLevel = 1;
    public int lastSeed;
    public double villageMinutes = VillageTime.MorningMinute;
    public string expeditionRunIdentity = "";
    public bool pendingRunResult;
    public bool resultSucceeded;
    public int resultSecuredGold;
    public int resultLostGold;
    public int resultLostItemCount;
    public int resultHealthUnits;
    public int resultNextThreat;
    public int[] expeditionLoot = new int[ItemInventory.PlayerSlotCount];
    public ItemStack[] playerItems = new ItemStack[ItemInventory.PlayerSlotCount];
    public ItemStack[] chestItems = new ItemStack[ItemInventory.ChestSlotCount];

    public static GameState FromLegacyPrefs()
    {
        var state = new GameState
        {
            health = Mathf.Clamp(PlayerPrefs.GetInt(ExpeditionPlayerHealth.HealthKey,
                ExpeditionPlayerHealth.DefaultMaxHealthUnits), 0, ExpeditionPlayerHealth.DefaultMaxHealthUnits),
            energy = Mathf.Clamp(PlayerPrefs.GetFloat(ExpeditionPlayerEnergy.EnergyKey,
                ExpeditionPlayerEnergy.DefaultStartingEnergy), 0f, ExpeditionPlayerEnergy.DefaultMaxEnergy),
            injured = PlayerPrefs.GetInt(ExpeditionPlayerHealth.InjuryKey, 0) == 1,
            completedRuns = Mathf.Max(0, PlayerPrefs.GetInt(ExpeditionRunProgression.CompletedRunsKey, 0)),
            pendingThreat = Mathf.Max(0, PlayerPrefs.GetInt(ExpeditionRunProgression.PendingThreatIncreaseKey, 0)),
            meleeUpgrade = PlayerPrefs.GetInt(PlayerProgression.ReinforcedMeleeKey, 0),
            infirmaryLevel = Mathf.Max(1, PlayerPrefs.GetInt(TownUpgradeBuilding.ProgressKey("infirmary"), 1)),
            lastSeed = PlayerPrefs.GetInt(ExpeditionSeedManager.LastSeedKey, 0),
            expeditionRunIdentity = PlayerPrefs.GetString(ExpeditionRunIdentity.PlayerPrefsKey, ""),
            villageMinutes = ParseMinutes(PlayerPrefs.GetString("Village.TotalMinutes", "480"))
        };
        if (!PlayerPrefs.HasKey(ExpeditionPlayerHealth.HealthKey) && state.injured)
            state.health = 0;
        state.gold = ItemInventory.GetTotal(InventoryItemId.Gold);
        state.pendingSecuredGold = Mathf.Max(0, PlayerPrefs.GetInt(TownHubController.PendingSecuredGoldKey, 0));
        state.playerItems = ItemInventory.ReadSlots(ItemInventory.Container.PlayerInventory);
        state.chestItems = ItemInventory.ReadSlots(ItemInventory.Container.HomeChest);
        string oldLootJson = PlayerPrefs.GetString("Expedition.CurrentLoot.v1", "");
        if (!string.IsNullOrEmpty(oldLootJson))
        {
            try
            {
                var legacyLoot = JsonUtility.FromJson<LegacyLoot>(oldLootJson);
                if (legacyLoot?.slots != null)
                    Array.Copy(legacyLoot.slots, state.expeditionLoot, Mathf.Min(legacyLoot.slots.Length, state.expeditionLoot.Length));
            }
            catch (Exception exception) { Debug.LogWarning($"Could not migrate carried expedition loot: {exception.Message}"); }
            PlayerPrefs.DeleteKey("Expedition.CurrentLoot.v1");
        }
        state.pendingRunResult = PlayerPrefs.GetInt("Expedition.PendingRunResult", 0) == 1;
        state.resultSucceeded = PlayerPrefs.GetInt("Expedition.ResultSuccess", 0) == 1;
        state.resultSecuredGold = PlayerPrefs.GetInt("Expedition.ResultSecuredGold", 0);
        state.resultLostGold = PlayerPrefs.GetInt("Expedition.ResultLostGold", 0);
        state.resultLostItemCount = PlayerPrefs.GetInt("Expedition.ResultLostItemCount", 0);
        state.resultHealthUnits = PlayerPrefs.GetInt("Expedition.ResultHealthUnits", 0);
        state.resultNextThreat = PlayerPrefs.GetInt("Expedition.ResultNextThreat", 0);
        return state;
    }

    public void ApplyRuntimeState()
    {
        PlayerPrefs.SetInt(ExpeditionPlayerHealth.HealthKey, health);
        PlayerPrefs.SetFloat(ExpeditionPlayerEnergy.EnergyKey, energy);
        PlayerPrefs.SetInt(ExpeditionPlayerHealth.InjuryKey, injured ? 1 : 0);
        PlayerPrefs.SetInt(ExpeditionRunProgression.CompletedRunsKey, completedRuns);
        PlayerPrefs.SetInt(ExpeditionRunProgression.PendingThreatIncreaseKey, pendingThreat);
        PlayerPrefs.SetInt(PlayerProgression.ReinforcedMeleeKey, meleeUpgrade);
        PlayerPrefs.SetInt(TownUpgradeBuilding.ProgressKey("infirmary"), infirmaryLevel);
        PlayerPrefs.SetInt(ExpeditionSeedManager.LastSeedKey, lastSeed);
        PlayerPrefs.SetString(ExpeditionRunIdentity.PlayerPrefsKey, expeditionRunIdentity ?? "");
        PlayerPrefs.SetString("Village.TotalMinutes", villageMinutes.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        PlayerPrefs.SetInt(TownHubController.GoldKey, gold);
        PlayerPrefs.SetInt(TownHubController.PendingSecuredGoldKey, pendingSecuredGold);
        ItemInventory.WriteSlots(ItemInventory.Container.PlayerInventory, playerItems);
        ItemInventory.WriteSlots(ItemInventory.Container.HomeChest, chestItems);
    }

    static double ParseMinutes(string value)
        => double.TryParse(value, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out double result)
            && !double.IsNaN(result) && !double.IsInfinity(result) && result >= 0d
            ? result : VillageTime.MorningMinute;

    [Serializable] sealed class LegacyLoot { public int[] slots; }
}
