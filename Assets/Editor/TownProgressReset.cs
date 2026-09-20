using UnityEditor;
using UnityEngine;

public static class TownProgressReset
{
    static readonly string[] UpgradeBuildingIds =
    {
        "infirmary"
    };

    [MenuItem("RPG/Reset Town Progress Now")]
    public static void ResetTownProgressNow() => ResetTownProgress(true);

    static void ResetTownProgress(bool logResult)
    {
        PlayerPrefs.DeleteKey(TownHubController.SuppliesKey);
        PlayerPrefs.DeleteKey(TownHubController.PendingSecuredSuppliesKey);
        PlayerPrefs.DeleteKey(ExpeditionPlayerHealth.InjuryKey);
        PlayerPrefs.DeleteKey(ExpeditionPlayerHealth.HealthKey);
        PlayerPrefs.DeleteKey(PlayerProgression.ReinforcedMeleeKey);
        foreach (var buildingId in UpgradeBuildingIds)
            PlayerPrefs.DeleteKey(TownUpgradeBuilding.ProgressKey(buildingId));

        PlayerPrefs.Save();
        if (logResult)
            Debug.Log("RPG_TOWN_PROGRESS_RESET");
    }
}
