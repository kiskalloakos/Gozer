using UnityEngine;

public class TownHubController : MonoBehaviour
{
    public const string SuppliesKey = "Town.Supplies";
    public static TownHubController Instance { get; private set; }

    [SerializeField] private int startingSupplies = 18;
    private string nearbyPrompt = "";
    private string notice = "Welcome home, Gozer.";
    private float noticeUntil;

    public int Supplies { get; private set; }

    void Awake()
    {
        Instance = this;
        Supplies = PlayerPrefs.GetInt(SuppliesKey, startingSupplies);
        ShowNotice("Welcome home, Gozer. The town is safe for now.");
    }

    public bool SpendSupplies(int amount)
    {
        if (Supplies < amount) return false;
        Supplies -= amount;
        PlayerPrefs.SetInt(SuppliesKey, Supplies);
        PlayerPrefs.Save();
        return true;
    }

    public void SetNearbyPrompt(string prompt) => nearbyPrompt = prompt;

    public void ShowNotice(string message, float seconds = 3.5f)
    {
        notice = message;
        noticeUntil = Time.time + seconds;
    }

    void OnGUI()
    {
        var oldColor = GUI.color;
        var title = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
        var body = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
        var centered = new GUIStyle(body) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };

        GUI.color = new Color(.08f, .055f, .04f, .92f);
        GUI.Box(new Rect(16, 16, 285, 82), GUIContent.none);
        GUI.color = Color.white;
        GUI.Label(new Rect(30, 24, 250, 30), "GOZER'S TOWN", title);
        GUI.Label(new Rect(30, 57, 250, 26), $"Town supplies: {Supplies}", body);

        if (!string.IsNullOrEmpty(nearbyPrompt))
        {
            GUI.color = new Color(.08f, .055f, .04f, .94f);
            GUI.Box(new Rect(Screen.width / 2f - 245, Screen.height - 82, 490, 48), GUIContent.none);
            GUI.color = new Color(1f, .88f, .52f);
            GUI.Label(new Rect(Screen.width / 2f - 235, Screen.height - 76, 470, 36), nearbyPrompt, centered);
        }

        if (Time.time < noticeUntil)
        {
            GUI.color = new Color(.14f, .09f, .055f, .94f);
            GUI.Box(new Rect(Screen.width / 2f - 280, 22, 560, 58), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width / 2f - 265, 28, 530, 46), notice, centered);
        }

        GUI.color = oldColor;
    }
}
