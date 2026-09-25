using UnityEngine;

public class TownHubController : MonoBehaviour
{
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
        GameState.InstallFromRuntime();
        // Town is the safe return point; unsecured loot can only exist during a run.
        if (CurrentExpeditionLoot.HasAny) CurrentExpeditionLoot.Lose();
        RemoveRetiredBuildings();
        Gold = ItemInventory.GetSecuredGoldTotal();
        GameState.Active.gold = Gold;
        IsInjured = GameState.Active.injured;
        HealthUnits = Mathf.Clamp(GameState.Active.health, 0, ExpeditionPlayerHealth.DefaultMaxHealthUnits);
        int securedGold = GameState.Active.pendingSecuredGold;
        GameState.Active.pendingSecuredGold = 0;
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
        if (!ItemInventory.TrySpendGold(amount, out remainingGold)) return false;
        if (Instance) Instance.Gold = remainingGold;
        return true;
    }

    public static int GetGoldBalance()
        => ItemInventory.GetSecuredGoldTotal();

    public static void AddSecuredGold(int amount)
    {
        if (amount <= 0) return;
        GameState.InstallFromRuntime();
        ItemInventory.AddItem(ItemInventory.Container.PlayerInventory, InventoryItemId.Gold, amount);
        GameState.Active.pendingSecuredGold += amount;
        GameState.Active.gold = ItemInventory.GetSecuredGoldTotal();
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
        GameState.Active.health = HealthUnits;
        GameState.Active.injured = false;
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
