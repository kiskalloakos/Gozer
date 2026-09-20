using UnityEngine;
using UnityEngine.SceneManagement;

public class ExpeditionHUD : MonoBehaviour
{
    public ExpeditionPlayerHealth health;
    [Range(1, 10)] public int inventorySlots = 6;

    public int CarriedLoot { get; private set; }

    string statusMessage = "";
    float statusUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InstallForCurrentAndFutureScenes()
    {
        SceneManager.sceneLoaded -= EnsureHUD;
        SceneManager.sceneLoaded += EnsureHUD;
        EnsureHUD(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void EnsureHUD(Scene scene, LoadSceneMode mode)
    {
        if (FindAnyObjectByType<ExpeditionHUD>()) return;
        new GameObject("Player HUD").AddComponent<ExpeditionHUD>();
    }

    void Awake()
    {
        if (!health) health = GetComponent<ExpeditionPlayerHealth>();
        if (!health) health = FindAnyObjectByType<ExpeditionPlayerHealth>();
    }

    public void AddLoot(int amount)
    {
        if (amount <= 0) return;
        CarriedLoot += amount;
        statusMessage = $"+{amount} expedition supplies";
        statusUntil = Time.time + 1.4f;
    }

    public int SecureLoot()
    {
        int secured = CarriedLoot;
        CarriedLoot = 0;
        if (secured > 0) TownHubController.AddSecuredSupplies(secured);
        return secured;
    }

    public void LoseLoot()
    {
        CarriedLoot = 0;
    }

    void OnGUI()
    {
        int maxHearts = health ? health.maxHearts : ExpeditionPlayerHealth.DefaultMaxHearts;
        int healthUnits = health
            ? health.CurrentHealth
            : Mathf.Clamp(
                PlayerPrefs.GetInt(ExpeditionPlayerHealth.HealthKey, ExpeditionPlayerHealth.DefaultMaxHealthUnits),
                0, ExpeditionPlayerHealth.DefaultMaxHealthUnits);

        const float slotSize = 50f;
        const float gap = 7f;
        float inventoryWidth = inventorySlots * slotSize + (inventorySlots - 1) * gap;
        float panelWidth = inventoryWidth + 34f;
        float panelX = (Screen.width - panelWidth) * .5f;
        float panelY = Screen.height - 92f;

        var oldColor = GUI.color;
        GUI.color = new Color(.035f, .04f, .055f, .94f);
        GUI.Box(new Rect(panelX, panelY, panelWidth, 78f), GUIContent.none);

        var label = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(.78f, .82f, .88f) }
        };
        GUI.color = Color.white;
        GUI.Label(new Rect(panelX, panelY + 2f, panelWidth, 18f), "INVENTORY", label);

        int securedSupplies = PlayerPrefs.GetInt(
            TownHubController.SuppliesKey,
            TownHubController.DefaultStartingSupplies);
        int displayedSupplies = securedSupplies + CarriedLoot;

        float slotsX = panelX + 17f;
        for (int i = 0; i < inventorySlots; i++)
        {
            var rect = new Rect(slotsX + i * (slotSize + gap), panelY + 21f, slotSize, slotSize);
            bool filled = i == 0 && displayedSupplies > 0;
            GUI.color = filled ? new Color(.95f, .67f, .22f, 1f) : new Color(.28f, .32f, .39f, 1f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = filled ? new Color(.31f, .2f, .06f, 1f) : new Color(.07f, .08f, .11f, 1f);
            GUI.Box(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(rect.x + 4f, rect.y + 2f, 14f, 16f), (i + 1).ToString(), label);
            if (filled)
            {
                var lootStyle = new GUIStyle(label) { fontSize = 22 };
                lootStyle.normal.textColor = new Color(1f, .76f, .24f);
                GUI.Label(new Rect(rect.x + 2f, rect.y + 10f, 28f, 32f), "◆", lootStyle);

                var stackStyle = new GUIStyle(label)
                {
                    alignment = TextAnchor.LowerRight,
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                };
                stackStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(rect.x + 17f, rect.y + 23f, 29f, 21f), $"×{displayedSupplies}", stackStyle);
            }
        }

        float heartsWidth = maxHearts * 30f;
        float heartsX = (Screen.width - heartsWidth) * .5f;
        float heartsY = panelY - 38f;
        HealthHeartGUI.Draw(healthUnits, maxHearts, heartsX, heartsY);

        if (Time.time < statusUntil)
        {
            GUI.color = new Color(1f, .78f, .28f);
            GUI.Label(new Rect(Screen.width * .5f - 150f, heartsY - 30f, 300f, 24f), statusMessage, label);
        }
        GUI.color = oldColor;
    }
}
