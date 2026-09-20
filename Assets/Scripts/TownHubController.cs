using UnityEngine;

public class TownHubController : MonoBehaviour
{
    public const int DefaultStartingSupplies = 18;
    static readonly string[] RetiredBuildingNames =
    {
        "STORAGE",
        "WATCHTOWER",
        "GREENHOUSE"
    };

    public const string SuppliesKey = "Town.Supplies";
    public const string PendingSecuredSuppliesKey = "Town.PendingSecuredSupplies";
    public static TownHubController Instance { get; private set; }

    [SerializeField] private int startingSupplies = DefaultStartingSupplies;
    private string notice = "Welcome home, Gozer.";
    private float noticeUntil;

    public int Supplies { get; private set; }
    public bool IsInjured { get; private set; }
    public int HealthUnits { get; private set; }
    public bool NeedsTreatment => IsInjured || HealthUnits < ExpeditionPlayerHealth.DefaultMaxHealthUnits;

    void Awake()
    {
        Instance = this;
        RemoveRetiredBuildings();
        if (!PlayerPrefs.HasKey(SuppliesKey))
            PlayerPrefs.SetInt(SuppliesKey, startingSupplies);
        Supplies = PlayerPrefs.GetInt(SuppliesKey, startingSupplies);
        IsInjured = PlayerPrefs.GetInt(ExpeditionPlayerHealth.InjuryKey, 0) == 1;
        if (!PlayerPrefs.HasKey(ExpeditionPlayerHealth.HealthKey))
            PlayerPrefs.SetInt(ExpeditionPlayerHealth.HealthKey,
                IsInjured ? 0 : ExpeditionPlayerHealth.DefaultMaxHealthUnits);
        HealthUnits = Mathf.Clamp(
            PlayerPrefs.GetInt(ExpeditionPlayerHealth.HealthKey, ExpeditionPlayerHealth.DefaultMaxHealthUnits),
            0, ExpeditionPlayerHealth.DefaultMaxHealthUnits);
        int securedSupplies = PlayerPrefs.GetInt(PendingSecuredSuppliesKey, 0);
        PlayerPrefs.DeleteKey(PendingSecuredSuppliesKey);
        PlayerPrefs.Save();
        ShowNotice(securedSupplies > 0
            ? $"Extraction successful — {securedSupplies} supplies secured."
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

    public bool SpendSupplies(int amount)
    {
        if (Supplies < amount) return false;
        Supplies -= amount;
        PlayerPrefs.SetInt(SuppliesKey, Supplies);
        PlayerPrefs.Save();
        return true;
    }

    public static void AddSecuredSupplies(int amount)
    {
        if (amount <= 0) return;
        int supplies = PlayerPrefs.GetInt(SuppliesKey, DefaultStartingSupplies);
        PlayerPrefs.SetInt(SuppliesKey, supplies + amount);
        PlayerPrefs.SetInt(PendingSecuredSuppliesKey,
            PlayerPrefs.GetInt(PendingSecuredSuppliesKey, 0) + amount);
        PlayerPrefs.Save();
    }

    public void TreatPlayer(int cost)
    {
        if (!NeedsTreatment)
        {
            ShowNotice("The infirmary checks you over — you are already at full health.");
            return;
        }

        if (!SpendSupplies(cost))
        {
            ShowNotice($"Not enough supplies. Treatment costs {cost}.");
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
        if (Time.time < noticeUntil)
        {
            var noticeStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = 14,
                fontStyle = FontStyle.Normal,
                wordWrap = true,
                normal = { textColor = new Color(.92f, .9f, .82f, .78f) }
            };
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width / 2f - 280f, 10f, 560f, 42f), notice, noticeStyle);
        }

        GUI.color = oldColor;
    }
}
