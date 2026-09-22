using UnityEngine;
using UnityEngine.Serialization;

public class TownHubController : MonoBehaviour
{
    public const int DefaultStartingGold = 18;
    static readonly string[] RetiredBuildingNames =
    {
        "STORAGE",
        "WATCHTOWER",
        "GREENHOUSE"
    };

    // Keep the original PlayerPrefs values so existing saves migrate to Gold without losing currency.
    public const string GoldKey = "Town.Supplies";
    public const string PendingSecuredGoldKey = "Town.PendingSecuredSupplies";
    public static TownHubController Instance { get; private set; }

    [FormerlySerializedAs("startingSupplies")]
    [SerializeField] private int startingGold = DefaultStartingGold;
    private string notice = "Welcome home, Gozer.";
    private float noticeUntil;
    private bool showingRunResult;

    public int Gold { get; private set; }
    public bool IsInjured { get; private set; }
    public int HealthUnits { get; private set; }
    public bool NeedsTreatment => IsInjured || HealthUnits < ExpeditionPlayerHealth.DefaultMaxHealthUnits;

    void Awake()
    {
        Instance = this;
        RemoveRetiredBuildings();
        if (!PlayerPrefs.HasKey(GoldKey))
            PlayerPrefs.SetInt(GoldKey, startingGold);
        Gold = GoldInventoryLocation.GetTotalGold();
        IsInjured = PlayerPrefs.GetInt(ExpeditionPlayerHealth.InjuryKey, 0) == 1;
        if (!PlayerPrefs.HasKey(ExpeditionPlayerHealth.HealthKey))
            PlayerPrefs.SetInt(ExpeditionPlayerHealth.HealthKey,
                IsInjured ? 0 : ExpeditionPlayerHealth.DefaultMaxHealthUnits);
        HealthUnits = Mathf.Clamp(
            PlayerPrefs.GetInt(ExpeditionPlayerHealth.HealthKey, ExpeditionPlayerHealth.DefaultMaxHealthUnits),
            0, ExpeditionPlayerHealth.DefaultMaxHealthUnits);
        int securedGold = PlayerPrefs.GetInt(PendingSecuredGoldKey, 0);
        PlayerPrefs.DeleteKey(PendingSecuredGoldKey);
        PlayerPrefs.Save();
        if (ExpeditionRunResult.TryConsume(out var result))
        {
            showingRunResult = true;
            gameObject.AddComponent<RunResultScreen>().Show(result);
        }
        bool threatIncreased = ExpeditionRunProgression.TryConsumePendingThreatIncrease(out int threatLevel);
        ShowNotice(threatIncreased
            ? securedGold > 0
                ? $"Extraction successful — {securedGold} Gold secured. Next threat level: {threatLevel}."
                : $"Extraction successful. Next threat level: {threatLevel}."
            : securedGold > 0
                ? $"Extraction successful — {securedGold} Gold secured."
            : IsInjured
                ? "You made it home injured. Visit the infirmary for treatment."
                : "Welcome home, Gozer. The town is safe for now.");
    }

    static void RemoveRetiredBuildings()
    {
        foreach (var buildingName in RetiredBuildingNames)
        {
            var building = GameObject.Find(buildingName);
            if (building) Destroy(building);
        }
    }

    public bool SpendGold(int amount)
    {
        if (!TrySpendGold(amount, out int remainingGold)) return false;
        Gold = remainingGold;
        return true;
    }

    public static bool TrySpendGold(int amount, out int remainingGold)
    {
        if (!GoldInventoryLocation.TrySpend(amount, out remainingGold)) return false;
        if (Instance) Instance.Gold = remainingGold;
        return true;
    }

    public static int GetGoldBalance()
        => GoldInventoryLocation.GetTotalGold();

    public static void AddSecuredGold(int amount)
    {
        if (amount <= 0) return;
        GoldInventoryLocation.AddAsNewPlayerStack(amount);
        PlayerPrefs.SetInt(PendingSecuredGoldKey,
            PlayerPrefs.GetInt(PendingSecuredGoldKey, 0) + amount);
        PlayerPrefs.Save();
        if (Instance) Instance.Gold = GetGoldBalance();
    }

    public void TreatPlayer(int cost)
    {
        if (!NeedsTreatment)
        {
            ShowNotice("The infirmary checks you over — you are already at full health.");
            return;
        }

        if (!SpendGold(cost))
        {
            ShowNotice($"Not enough Gold. Treatment costs {cost} Gold.");
            return;
        }

        HealthUnits = ExpeditionPlayerHealth.DefaultMaxHealthUnits;
        IsInjured = false;
        PlayerPrefs.SetInt(ExpeditionPlayerHealth.HealthKey, HealthUnits);
        PlayerPrefs.SetInt(ExpeditionPlayerHealth.InjuryKey, 0);
        PlayerPrefs.Save();
        ShowNotice($"Treatment complete — health restored to {ExpeditionPlayerHealth.DefaultMaxHearts} hearts.", 4.5f);
    }

    public bool CanBeginExpedition()
    {
        if (HealthUnits > 0) return true;
        ShowNotice("You are too injured to leave town. Visit the infirmary first.", 4.5f);
        return false;
    }

    public void ShowNotice(string message, float seconds = 3.5f)
    {
        notice = message;
        noticeUntil = Time.time + seconds;
    }

    void OnGUI()
    {
        var oldColor = GUI.color;
        if (!showingRunResult && Time.time < noticeUntil)
        {
            GUI.color = Color.white;
            ExpeditionHUD.DrawPixelTextCentered(notice, 10f, 2f);
        }

        GUI.color = oldColor;
    }
}
