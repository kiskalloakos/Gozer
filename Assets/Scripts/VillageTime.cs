using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// One saved clock shared by the village and home. Expeditions consume the night.
public sealed class VillageTime : MonoBehaviour
{
    const string MinutesKey = "Village.TotalMinutes";
    const string ExpeditionKey = "Village.ExpeditionInProgress";
    public const float RealSecondsPerDay = 600f;
    public const int MorningMinute = 8 * 60;
    public const int NightMinute = 19 * 60;
    public static VillageTime Instance { get; private set; }
    public double TotalMinutes { get; private set; }
    public int Day => (int)(TotalMinutes / 1440) + 1;
    public int MinuteOfDay => (int)(TotalMinutes % 1440);
    public bool IsNight => MinuteOfDay >= NightMinute || MinuteOfDay < MorningMinute;
    public bool IsSleeping { get; private set; }
    float saveTimer;
    float fade;
    float previousTimeScale;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        if (!Instance) new GameObject("Village Clock").AddComponent<VillageTime>();
    }

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        string saved = PlayerPrefs.GetString(MinutesKey, "480");
        TotalMinutes = double.TryParse(saved, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out double minutes)
            && !double.IsNaN(minutes) && !double.IsInfinity(minutes) && minutes >= 0 ? minutes : MorningMinute;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "ExpeditionField")
        {
            PlayerPrefs.SetInt(ExpeditionKey, 1);
            Save();
        }
        else if (IsVillage(scene.name) && PlayerPrefs.GetInt(ExpeditionKey, 0) == 1)
            AdvanceToMorning();

        if (scene.name == "HomeInterior")
        {
            var sofa = GameObject.Find("Sofa");
            if (sofa && !sofa.GetComponent<HomeBed>()) sofa.AddComponent<HomeBed>();
        }
        Save();
    }

    static bool IsVillage(string scene) => scene == "TownHub" || scene == "HomeInterior";

    void Update()
    {
        if (IsSleeping || !IsVillage(SceneManager.GetActiveScene().name)) return;
        TotalMinutes += Time.deltaTime * 1440d / RealSecondsPerDay;
        saveTimer += Time.unscaledDeltaTime;
        if (saveTimer >= 5f) { Save(); saveTimer = 0f; }
    }

    public bool CanEnterExpedition()
    {
        if (IsSleeping) return false;
        if (IsNight && PlayerPrefs.GetInt(ExpeditionKey, 0) == 0) return true;
        Notify("It’s not time for an expedition yet. Come back at 7:00 PM.");
        return false;
    }

    public void Sleep()
    {
        if (IsSleeping) return;
        if (!IsNight) { Notify("It’s not time to sleep yet. You can sleep from 7:00 PM."); return; }
        StartCoroutine(SleepThroughNight());
    }

    IEnumerator SleepThroughNight()
    {
        IsSleeping = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        yield return FadeTo(1f);
        yield return new WaitForSecondsRealtime(1f);
        AdvanceToMorning();
        // The player stays at their safe interaction position beside the sofa.
        yield return FadeTo(0f);
        Time.timeScale = previousTimeScale;
        IsSleeping = false;
        Notify("Good morning! It’s 8:00 AM.");
    }

    IEnumerator FadeTo(float target)
    {
        while (!Mathf.Approximately(fade, target))
        {
            fade = Mathf.MoveTowards(fade, target, Time.unscaledDeltaTime / .65f);
            yield return null;
        }
    }

    public void AdvanceToMorning()
    {
        double midnight = System.Math.Floor(TotalMinutes / 1440d) * 1440d;
        TotalMinutes = midnight + MorningMinute + (MinuteOfDay >= MorningMinute ? 1440d : 0d);
        PlayerPrefs.DeleteKey(ExpeditionKey);
        Save();
    }

    /// <summary>Synchronizes the persistent clock after a save-slot restore.</summary>
    public void RestoreSavedTime(double totalMinutes)
    {
        TotalMinutes = !double.IsNaN(totalMinutes) && !double.IsInfinity(totalMinutes)
            && totalMinutes >= 0d ? totalMinutes : MorningMinute;
        saveTimer = 0f;
    }

    public void Save()
    {
        PlayerPrefs.SetString(MinutesKey, TotalMinutes.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        PlayerPrefs.Save();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only clock hook used by the RPG/Tests menu. It keeps the current
    /// in-game day and moves the village clock to the requested minute.
    /// </summary>
    public void SetForTesting(int minuteOfDay)
    {
        int clampedMinute = Mathf.Clamp(minuteOfDay, 0, 1439);
        double dayStart = System.Math.Floor(TotalMinutes / 1440d) * 1440d;
        TotalMinutes = dayStart + clampedMinute;
        Save();
    }
#endif

    public static void ResetSavedClock()
    {
        PlayerPrefs.DeleteKey(MinutesKey);
        PlayerPrefs.DeleteKey(ExpeditionKey);
        if (Instance) Instance.TotalMinutes = MorningMinute;
        PlayerPrefs.Save();
    }

    static void Notify(string message)
    {
        if (HomeInteriorController.Instance) HomeInteriorController.Instance.ShowNotice(message);
        else if (TownHubController.Instance) TownHubController.Instance.ShowNotice(message);
    }

    void OnApplicationPause(bool paused) { if (paused) Save(); }
    void OnApplicationQuit() => Save();
    void OnDestroy()
    {
        if (Instance != this) return;
        if (IsSleeping) Time.timeScale = previousTimeScale;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    void OnGUI()
    {
        if (!IsVillage(SceneManager.GetActiveScene().name)) return;
        int hour = MinuteOfDay / 60;
        string clock = $"DAY {Day}  {(hour % 12 == 0 ? 12 : hour % 12)}:{MinuteOfDay % 60:00} {(hour < 12 ? "AM" : "PM")}";
        ExpeditionHUD.DrawPixelTextCentered(clock, 58f, 2f);
        if (!IsSleeping) return;
        int oldDepth = GUI.depth;
        Color oldColor = GUI.color;
        GUI.depth = -10000;
        GUI.color = new Color(0f, 0f, 0f, fade);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = new Color(1f, 1f, 1f, fade);
        GUI.Label(new Rect(0, Screen.height / 2f - 20f, Screen.width, 40f),
            "SLEEPING…", ExpeditionHUD.CreateLabelStyle(20));
        GUI.color = oldColor;
        GUI.depth = oldDepth;
    }
}
