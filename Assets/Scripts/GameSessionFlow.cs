using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Owns the game-wide front end. It deliberately keeps the existing gameplay
/// PlayerPrefs keys intact: each save slot is a snapshot of those keys, which
/// lets the rest of the game continue to use its simple persistence API.
/// </summary>
public sealed class GameSessionFlow : MonoBehaviour
{
    const int SlotCount = 3;
    const string ActiveSlotKey = "Session.ActiveSlot";
    const string SlotKeyPrefix = "Session.SaveSlot.";
    const string VolumeKey = "Session.MasterVolume";
    const string FullscreenKey = "Session.Fullscreen";
    public const string EditorPreviewKey = "Session.EditorExpeditionPreview";

    enum MenuScreen { Gameplay, Title, Slots, Pause, Settings, ConfirmQuit, ConfirmSlotAction }
    enum SlotAction { None, NewGame, Delete }

    [Serializable] struct SaveData
    {
        public bool exists;
        public string savedAt;
        public int gold;
        public int health;
        public int completedRuns;
        public float villageMinutes;
        public int[] playerGold;
        public int[] chestGold;
        public int injured;
        public int meleeUpgrade;
        public int infirmaryLevel;
        public int lastSeed;
        public string expeditionRunIdentity;
    }

    static GameSessionFlow instance;
    MenuScreen screen;
    MenuScreen returnScreen;
    int activeSlot = -1;
    int pendingSlot = -1;
    SlotAction pendingSlotAction;
    float preMenuTimeScale = 1f;

    public static bool IsBlockingGameplay => instance && instance.screen != MenuScreen.Gameplay;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        if (instance) return;
        new GameObject("Game Session Flow").AddComponent<GameSessionFlow>();
    }

    void Awake()
    {
        if (instance && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        activeSlot = PlayerPrefs.GetInt(ActiveSlotKey, -1);
        // Preserve progress made before save slots were introduced by promoting
        // the old, unscoped PlayerPrefs data to the first slot on first launch.
        if (!PlayerPrefs.HasKey(ActiveSlotKey) && HasLegacyGameState())
        {
            GoldInventoryLocation.GetTotalGold(); // complete legacy Gold migration before snapshotting
            activeSlot = 0;
            PlayerPrefs.SetInt(ActiveSlotKey, activeSlot);
            SaveActiveSlot();
        }
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
#if UNITY_EDITOR
        if (PlayerPrefs.GetInt(EditorPreviewKey, 0) == 1)
        {
            // Editor seed previews load the expedition directly and must not be
            // stopped behind the normal title screen.
            PlayerPrefs.DeleteKey(EditorPreviewKey);
            PlayerPrefs.Save();
            screen = MenuScreen.Gameplay;
            Time.timeScale = 1f;
        }
        else
        {
            Open(MenuScreen.Title);
        }
#else
        Open(MenuScreen.Title);
#endif
    }

    void Update()
    {
        if (screen == MenuScreen.Title || screen == MenuScreen.Slots || screen == MenuScreen.Settings
            || screen == MenuScreen.ConfirmQuit || screen == MenuScreen.ConfirmSlotAction)
            return;

        if (screen == MenuScreen.Pause)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Resume();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) && !PlayerInventoryUI.IsOpen && !HomeStorageChest.IsModalOpen)
            Open(MenuScreen.Pause);
    }

    void OnApplicationPause(bool paused) { if (paused) SaveActiveSlot(); }
    void OnApplicationQuit() => SaveActiveSlot();

    void Open(MenuScreen next)
    {
        if (screen == next) return;
        if (screen == MenuScreen.Gameplay) preMenuTimeScale = Time.timeScale;
        screen = next;
        Time.timeScale = 0f;
    }

    void Resume()
    {
        screen = MenuScreen.Gameplay;
        Time.timeScale = Mathf.Approximately(preMenuTimeScale, 0f) ? 1f : preMenuTimeScale;
    }

    void OnGUI()
    {
        if (screen == MenuScreen.Gameplay) return;
        GUI.depth = -10000;
        var old = GUI.color;
        GUI.color = new Color(.015f, .012f, .025f, .88f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;

        switch (screen)
        {
            case MenuScreen.Title: DrawTitle(); break;
            case MenuScreen.Slots: DrawSlots(); break;
            case MenuScreen.Pause: DrawPause(); break;
            case MenuScreen.Settings: DrawSettings(); break;
            case MenuScreen.ConfirmQuit: DrawConfirmQuit(); break;
            case MenuScreen.ConfirmSlotAction: DrawConfirmSlotAction(); break;
        }
    }

    void DrawTitle()
    {
        Rect box = Panel(350f, 370f);
        Label("RPG EXPEDITIONS", new Rect(box.x, box.y + 38, box.width, 42), 27, new Color(1f, .78f, .3f));
        Label("TOWN. TREASURE. SURVIVAL.", new Rect(box.x, box.y + 84, box.width, 22), 12, new Color(.78f, .78f, .88f));
        bool canContinue = activeSlot >= 0 && LoadSlot(activeSlot).exists;
        if (Button("CONTINUE", box, 130, !canContinue)) { RestoreSlot(activeSlot); Resume(); }
        if (Button("SAVE SLOTS", box, 184)) Open(MenuScreen.Slots);
        if (Button("SETTINGS", box, 238)) { returnScreen = MenuScreen.Title; Open(MenuScreen.Settings); }
        if (Button("QUIT GAME", box, 292)) { returnScreen = MenuScreen.Title; Open(MenuScreen.ConfirmQuit); }
        Label(canContinue ? $"ACTIVE SLOT {activeSlot + 1}" : "CHOOSE A SAVE SLOT TO BEGIN",
            new Rect(box.x, box.y + 335, box.width, 18), 11, new Color(.7f, .7f, .78f));
    }

    void DrawSlots()
    {
        Rect box = Panel(520f, 425f);
        Label("SAVE SLOTS", new Rect(box.x, box.y + 24, box.width, 32), 23, new Color(1f, .78f, .3f));
        for (int i = 0; i < SlotCount; i++)
        {
            SaveData data = LoadSlot(i);
            float y = box.y + 72 + i * 88;
            string details = data.exists
                ? $"{data.savedAt}   GOLD {data.gold}   RUNS {data.completedRuns}"
                : "EMPTY SLOT — START A NEW GAME";
            if (GUI.Button(new Rect(box.x + 24, y, 350, 55), $"SLOT {i + 1}\n{details}"))
            {
                if (data.exists)
                {
                    SaveActiveSlot();
                    RestoreSlot(i);
                    Resume();
                }
                else ConfirmSlotAction(i, SlotAction.NewGame);
            }
            if (GUI.Button(new Rect(box.x + 388, y, 105, 55), data.exists ? "DELETE" : "NEW"))
            {
                ConfirmSlotAction(i, data.exists ? SlotAction.Delete : SlotAction.NewGame);
            }
        }
        if (GUI.Button(new Rect(box.x + 24, box.y + 352, 220, 42), "BACK")) Open(MenuScreen.Title);
        Label("Selecting an existing slot loads it. Saves are captured automatically when pausing or leaving the game.",
            new Rect(box.x + 24, box.y + 398, box.width - 48, 18), 10, new Color(.72f, .72f, .8f));
    }

    void DrawPause()
    {
        Rect box = Panel(330f, 306f);
        Label("PAUSED", new Rect(box.x, box.y + 28, box.width, 36), 26, new Color(1f, .78f, .3f));
        if (Button("RESUME", box, 88)) Resume();
        if (Button("SAVE & TITLE", box, 142)) { SaveActiveSlot(); Open(MenuScreen.Title); }
        if (Button("SETTINGS", box, 196)) { returnScreen = MenuScreen.Pause; Open(MenuScreen.Settings); }
        if (Button("QUIT GAME", box, 250)) { returnScreen = MenuScreen.Pause; Open(MenuScreen.ConfirmQuit); }
    }

    void DrawSettings()
    {
        Rect box = Panel(430f, 295f);
        Label("SETTINGS", new Rect(box.x, box.y + 30, box.width, 34), 24, new Color(1f, .78f, .3f));
        Label("MASTER VOLUME", new Rect(box.x + 34, box.y + 100, 180, 22), 13, Color.white);
        float volume = GUI.HorizontalSlider(new Rect(box.x + 34, box.y + 130, 270, 22), AudioListener.volume, 0f, 1f);
        if (!Mathf.Approximately(volume, AudioListener.volume))
        {
            AudioListener.volume = volume;
            PlayerPrefs.SetFloat(VolumeKey, volume);
            PlayerPrefs.Save();
        }
        Label($"{Mathf.RoundToInt(volume * 100)}%", new Rect(box.x + 316, box.y + 126, 70, 24), 13, Color.white);
        bool fullscreen = Screen.fullScreen;
        if (GUI.Toggle(new Rect(box.x + 34, box.y + 172, 200, 28), fullscreen, "FULLSCREEN") != fullscreen)
        {
            Screen.fullScreen = !fullscreen;
            PlayerPrefs.SetInt(FullscreenKey, Screen.fullScreen ? 1 : 0);
            PlayerPrefs.Save();
        }
        if (GUI.Button(new Rect(box.x + 34, box.y + 232, 150, 38), "BACK")) Open(returnScreen);
    }

    void DrawConfirmQuit()
    {
        Rect box = Panel(390f, 210f);
        Label("LEAVE THE GAME?", new Rect(box.x, box.y + 30, box.width, 34), 22, new Color(1f, .68f, .4f));
        Label("Your active save slot will be saved.", new Rect(box.x, box.y + 82, box.width, 24), 12, Color.white);
        if (GUI.Button(new Rect(box.x + 28, box.y + 140, 150, 40), "CANCEL")) Open(returnScreen);
        if (GUI.Button(new Rect(box.x + 212, box.y + 140, 150, 40), "QUIT")) Quit();
    }

    void ConfirmSlotAction(int slot, SlotAction action)
    {
        pendingSlot = slot;
        pendingSlotAction = action;
        returnScreen = MenuScreen.Slots;
        Open(MenuScreen.ConfirmSlotAction);
    }

    void DrawConfirmSlotAction()
    {
        bool isNewGame = pendingSlotAction == SlotAction.NewGame;
        Rect box = Panel(420f, 235f);
        Label(isNewGame ? "START A NEW GAME?" : "DELETE THIS SAVE?",
            new Rect(box.x, box.y + 28, box.width, 34), 21, new Color(1f, .68f, .4f));
        string message = isNewGame
            ? $"Slot {pendingSlot + 1} will begin at day one. Your current active slot will be saved first."
            : $"Slot {pendingSlot + 1} will be permanently removed.";
        Label(message, new Rect(box.x + 28, box.y + 78, box.width - 56, 46), 12, Color.white);
        if (GUI.Button(new Rect(box.x + 30, box.y + 166, 160, 40), "CANCEL"))
        {
            pendingSlotAction = SlotAction.None;
            Open(MenuScreen.Slots);
        }
        if (GUI.Button(new Rect(box.x + 230, box.y + 166, 160, 40), isNewGame ? "START NEW" : "DELETE"))
        {
            if (pendingSlotAction == SlotAction.NewGame)
            {
                SaveActiveSlot();
                StartNewGame(pendingSlot);
                Resume();
            }
            else if (pendingSlotAction == SlotAction.Delete)
            {
                DeleteSlot(pendingSlot);
                Open(MenuScreen.Slots);
            }
            pendingSlotAction = SlotAction.None;
        }
    }

    bool Button(string text, Rect box, float y, bool disabled = false)
    {
        GUI.enabled = !disabled;
        bool clicked = GUI.Button(new Rect(box.x + 35, box.y + y, box.width - 70, 43), text);
        GUI.enabled = true;
        return clicked;
    }

    static Rect Panel(float width, float height) => new Rect((Screen.width - width) * .5f, (Screen.height - height) * .5f, width, height);
    static void Label(string value, Rect rect, int size, Color color)
    {
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = size, fontStyle = FontStyle.Bold, wordWrap = true };
        style.normal.textColor = color;
        GUI.Label(rect, value, style);
    }

    void StartNewGame(int slot)
    {
        ClearGameState();
        activeSlot = slot;
        PlayerPrefs.SetInt(ActiveSlotKey, slot);
        PlayerPrefs.Save();
        SceneManager.LoadScene(GameSceneCatalog.Name(GameScene.TownHub));
        SaveActiveSlot();
    }

    void Quit()
    {
        SaveActiveSlot();
#if UNITY_EDITOR
        Debug.Log("QUIT GAME requested. Application.Quit is ignored in the Unity Editor.");
#else
        Application.Quit();
#endif
    }

    void SaveActiveSlot()
    {
        if (activeSlot < 0 || activeSlot >= SlotCount) return;
        var data = new SaveData
        {
            exists = true,
            savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            gold = PlayerPrefs.GetInt(TownHubController.GoldKey, TownHubController.DefaultStartingGold),
            health = PlayerPrefs.GetInt(ExpeditionPlayerHealth.HealthKey, ExpeditionPlayerHealth.DefaultMaxHealthUnits),
            completedRuns = PlayerPrefs.GetInt(ExpeditionRunProgression.CompletedRunsKey, 0),
            villageMinutes = ParseFloat(PlayerPrefs.GetString("Village.TotalMinutes", "480"), 480f),
            injured = PlayerPrefs.GetInt(ExpeditionPlayerHealth.InjuryKey, 0),
            meleeUpgrade = PlayerPrefs.GetInt(PlayerProgression.ReinforcedMeleeKey, 0),
            infirmaryLevel = PlayerPrefs.GetInt(TownUpgradeBuilding.ProgressKey("infirmary"), 1),
            lastSeed = PlayerPrefs.GetInt(ExpeditionSeedManager.LastSeedKey, 0),
            expeditionRunIdentity = PlayerPrefs.GetString(ExpeditionRunIdentity.PlayerPrefsKey, ""),
            playerGold = ReadGold(GoldInventoryLocation.Container.PlayerInventory),
            chestGold = ReadGold(GoldInventoryLocation.Container.HomeChest)
        };
        PlayerPrefs.SetString(SlotKey(activeSlot), JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    void RestoreSlot(int slot)
    {
        SaveData data = LoadSlot(slot);
        if (!data.exists) return;
        ClearGameState();
        PlayerPrefs.SetInt(TownHubController.GoldKey, data.gold);
        PlayerPrefs.SetInt(ExpeditionPlayerHealth.HealthKey, data.health);
        PlayerPrefs.SetInt(ExpeditionPlayerHealth.InjuryKey, data.injured);
        PlayerPrefs.SetInt(ExpeditionRunProgression.CompletedRunsKey, data.completedRuns);
        PlayerPrefs.SetInt(PlayerProgression.ReinforcedMeleeKey, data.meleeUpgrade);
        PlayerPrefs.SetInt(TownUpgradeBuilding.ProgressKey("infirmary"), data.infirmaryLevel);
        PlayerPrefs.SetInt(ExpeditionSeedManager.LastSeedKey, data.lastSeed);
        ExpeditionRunIdentity.RestoreSerialized(data.expeditionRunIdentity);
        PlayerPrefs.SetString("Village.TotalMinutes", data.villageMinutes.ToString(System.Globalization.CultureInfo.InvariantCulture));
        WriteGold(GoldInventoryLocation.Container.PlayerInventory, data.playerGold);
        WriteGold(GoldInventoryLocation.Container.HomeChest, data.chestGold);
        PlayerPrefs.SetInt(GoldInventoryLocation.StackMigrationKey, 1);
        activeSlot = slot;
        PlayerPrefs.SetInt(ActiveSlotKey, slot);
        PlayerPrefs.Save();
        SceneManager.LoadScene(GameSceneCatalog.Name(GameScene.TownHub));
    }

    SaveData LoadSlot(int slot)
    {
        string json = PlayerPrefs.GetString(SlotKey(slot), "");
        return string.IsNullOrEmpty(json) ? default : JsonUtility.FromJson<SaveData>(json);
    }

    void DeleteSlot(int slot)
    {
        PlayerPrefs.DeleteKey(SlotKey(slot));
        if (activeSlot == slot) { activeSlot = -1; PlayerPrefs.SetInt(ActiveSlotKey, -1); }
        PlayerPrefs.Save();
    }

    static string SlotKey(int slot) => SlotKeyPrefix + slot;
    static int[] ReadGold(GoldInventoryLocation.Container container)
    {
        int count = container == GoldInventoryLocation.Container.PlayerInventory ? GoldInventoryLocation.PlayerSlotCount : GoldInventoryLocation.ChestSlotCount;
        var values = new int[count];
        for (int i = 0; i < count; i++) values[i] = PlayerPrefs.GetInt(GoldInventoryLocation.StackKey(container, i), 0);
        return values;
    }
    static void WriteGold(GoldInventoryLocation.Container container, int[] values)
    {
        int count = container == GoldInventoryLocation.Container.PlayerInventory ? GoldInventoryLocation.PlayerSlotCount : GoldInventoryLocation.ChestSlotCount;
        for (int i = 0; i < count; i++)
            if (values != null && i < values.Length && values[i] > 0) PlayerPrefs.SetInt(GoldInventoryLocation.StackKey(container, i), values[i]);
    }
    static float ParseFloat(string value, float fallback)
        => float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float result) ? result : fallback;
    static bool HasLegacyGameState()
        => PlayerPrefs.HasKey(TownHubController.GoldKey)
            || PlayerPrefs.HasKey(ExpeditionPlayerHealth.HealthKey)
            || PlayerPrefs.HasKey("Village.TotalMinutes")
            || PlayerPrefs.HasKey(ExpeditionRunProgression.CompletedRunsKey);
    static void ClearGameState()
    {
        VillageTime.ResetSavedClock();
        GoldInventoryLocation.ResetSavedState();
        string[] keys = { TownHubController.GoldKey, TownHubController.PendingSecuredGoldKey, ExpeditionPlayerHealth.HealthKey,
            ExpeditionPlayerHealth.InjuryKey, PlayerProgression.ReinforcedMeleeKey, ExpeditionRunProgression.CompletedRunsKey,
            ExpeditionRunProgression.PendingThreatIncreaseKey, ExpeditionSeedManager.LastSeedKey, "Expedition.PendingSeed", "Expedition.HasPendingSeed",
            "Expedition.PendingSeedSource", "Expedition.PendingDailyDate",
            "Expedition.PendingRunResult", "Expedition.ResultSuccess", "Expedition.ResultSecuredGold", "Expedition.ResultLostGold", "Expedition.ResultHealthUnits", "Expedition.ResultNextThreat" };
        foreach (string key in keys) PlayerPrefs.DeleteKey(key);
        PlayerPrefs.DeleteKey(TownUpgradeBuilding.ProgressKey("infirmary"));
        ExpeditionRunIdentity.Clear();
        PlayerPrefs.Save();
    }
}
