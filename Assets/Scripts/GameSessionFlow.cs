using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Owns the game-wide front end. Runtime systems still use PlayerPrefs as a
/// compatibility layer, but complete save snapshots are stored as independent
/// files so saves are unlimited and isolated from one another.
/// </summary>
public sealed class GameSessionFlow : MonoBehaviour
{
    const string ActiveSaveKey = "Session.ActiveSaveId";
    const string SaveDirectoryName = "Saves";
    const string SaveFilePrefix = "save-";
    const string SaveFileSuffix = ".json";
    const string LegacySlotKeyPrefix = "Session.SaveSlot.";
    const string VolumeKey = "Session.MasterVolume";
    const string FullscreenKey = "Session.Fullscreen";
    public const string EditorPreviewKey = "Session.EditorExpeditionPreview";

    enum MenuScreen { Gameplay, Title, Saves, Pause, Settings, ConfirmQuit, ConfirmSaveAction }
    enum SaveAction { None, NewGame, Delete }

    [Serializable] struct SaveData
    {
        public bool exists;
        public string savedAt;
        public string saveId;
        public string displayName;
        public int gold;
        public int health;
        public float energy;
        public bool energySaved;
        public int wood;
        public bool itemsSaved;
        public ItemStack[] playerItems;
        public ItemStack[] chestItems;
        public bool woodStacksSaved;
        public int[] playerWood;
        public int[] chestWood;
        public int playerAxeSlot;
        public int chestAxeSlot;
        public bool toolsSaved;
        public int completedRuns;
        public float villageMinutes;
        public int[] playerGold;
        public int[] chestGold;
        public int injured;
        public int meleeUpgrade;
        public int infirmaryLevel;
        public int lastSeed;
        public string expeditionRunIdentity;
        public GameState gameState;
    }

    static GameSessionFlow instance;
    MenuScreen screen;
    MenuScreen returnScreen;
    string activeSaveId;
    string pendingSaveId;
    SaveAction pendingSaveAction;
    float preMenuTimeScale = 1f;
    float autosaveElapsed;
    Vector2 savesScroll;

    public static bool IsBlockingGameplay => instance && instance.screen != MenuScreen.Gameplay;
    public static string ActiveSaveId => instance ? instance.activeSaveId : PlayerPrefs.GetString(ActiveSaveKey, "");
    public static void SaveActiveGameNow() { if (instance) instance.SaveActiveGame(); }

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
        GameState.InstallFromRuntime();
        activeSaveId = PlayerPrefs.GetString(ActiveSaveKey, "");
        MigrateLegacySlotSaves();
        // Preserve progress made before file saves were introduced by creating
        // one migrated save from the old unscoped PlayerPrefs data.
        if (string.IsNullOrEmpty(activeSaveId) && HasLegacyGameState())
        {
            ItemInventory.GetTotal(InventoryItemId.Gold); // complete legacy item migration before snapshotting
            activeSaveId = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(ActiveSaveKey, activeSaveId);
            SaveActiveGame();
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
        if (screen == MenuScreen.Gameplay && !string.IsNullOrEmpty(activeSaveId))
        {
            autosaveElapsed += Time.unscaledDeltaTime;
            if (autosaveElapsed >= 20f)
            {
                autosaveElapsed = 0f;
                SaveActiveGame();
            }
        }
        if (screen == MenuScreen.Title || screen == MenuScreen.Saves || screen == MenuScreen.Settings
            || screen == MenuScreen.ConfirmQuit || screen == MenuScreen.ConfirmSaveAction)
            return;

        if (screen == MenuScreen.Pause)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Resume();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) && !PlayerInventoryUI.IsOpen
            && !HomeStorageChest.IsModalOpen && !WorkbenchCraftingUI.IsModalOpen)
            Open(MenuScreen.Pause);
    }

    void OnApplicationPause(bool paused) { if (paused) SaveActiveGame(); }
    void OnApplicationQuit() => SaveActiveGame();

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
            case MenuScreen.Saves: DrawSaves(); break;
            case MenuScreen.Pause: DrawPause(); break;
            case MenuScreen.Settings: DrawSettings(); break;
            case MenuScreen.ConfirmQuit: DrawConfirmQuit(); break;
            case MenuScreen.ConfirmSaveAction: DrawConfirmSaveAction(); break;
        }
    }

    void DrawTitle()
    {
        Rect box = Panel(350f, 370f);
        Label("RPG EXPEDITIONS", new Rect(box.x, box.y + 38, box.width, 42), 27, new Color(1f, .78f, .3f));
        Label("TOWN. TREASURE. SURVIVAL.", new Rect(box.x, box.y + 84, box.width, 22), 12, new Color(.78f, .78f, .88f));
        bool canContinue = !string.IsNullOrEmpty(activeSaveId) && LoadSave(activeSaveId).exists;
        if (Button("CONTINUE", box, 130, !canContinue)) { RestoreSave(activeSaveId); Resume(); }
        if (Button("SAVED GAMES", box, 184)) Open(MenuScreen.Saves);
        if (Button("SETTINGS", box, 238)) { returnScreen = MenuScreen.Title; Open(MenuScreen.Settings); }
        if (Button("QUIT GAME", box, 292)) { returnScreen = MenuScreen.Title; Open(MenuScreen.ConfirmQuit); }
        Label(canContinue ? "ACTIVE SAVE READY" : "CREATE A SAVE TO BEGIN",
            new Rect(box.x, box.y + 335, box.width, 18), 11, new Color(.7f, .7f, .78f));
    }

    void DrawSaves()
    {
        List<SaveData> saves = ListSaves();
        float height = Mathf.Min(520f, Screen.height - 30f);
        Rect box = Panel(560f, height);
        Label("SAVED GAMES", new Rect(box.x, box.y + 18, box.width, 32), 23, new Color(1f, .78f, .3f));
        Rect viewport = new Rect(box.x + 20, box.y + 58, box.width - 40, height - 128);
        float contentHeight = Mathf.Max(viewport.height, saves.Count * 58f);
        savesScroll = GUI.BeginScrollView(viewport, savesScroll,
            new Rect(0f, 0f, viewport.width - 18f, contentHeight));
        for (int i = 0; i < saves.Count; i++)
        {
            SaveData data = saves[i];
            float y = i * 58f;
            string details = $"{data.displayName}   {data.savedAt}   GOLD {data.gold}   RUNS {data.completedRuns}";
            if (GUI.Button(new Rect(0, y, viewport.width - 140, 48), details))
            {
                SaveActiveGame();
                RestoreSave(data.saveId);
                Resume();
            }
            if (GUI.Button(new Rect(viewport.width - 128, y, 105, 48), "DELETE")) ConfirmSaveAction(data.saveId, SaveAction.Delete);
        }
        GUI.EndScrollView();
        float buttonY = box.yMax - 56;
        if (GUI.Button(new Rect(box.x + 24, buttonY, 220, 42), "NEW SAVE")) ConfirmSaveAction(Guid.NewGuid().ToString("N"), SaveAction.NewGame);
        if (GUI.Button(new Rect(box.x + 270, buttonY, 160, 42), "BACK")) Open(MenuScreen.Title);
        Label("Saves are stored independently. You can create as many as your disk can hold.",
            new Rect(box.x + 24, box.yMax - 28, box.width - 48, 18), 10, new Color(.72f, .72f, .8f));
    }

    void DrawPause()
    {
        Rect box = Panel(330f, 306f);
        Label("PAUSED", new Rect(box.x, box.y + 28, box.width, 36), 26, new Color(1f, .78f, .3f));
        if (Button("RESUME", box, 88)) Resume();
        if (Button("SAVE & TITLE", box, 142)) { SaveActiveGame(); Open(MenuScreen.Title); }
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
        Label("Your active save will be saved.", new Rect(box.x, box.y + 82, box.width, 24), 12, Color.white);
        if (GUI.Button(new Rect(box.x + 28, box.y + 140, 150, 40), "CANCEL")) Open(returnScreen);
        if (GUI.Button(new Rect(box.x + 212, box.y + 140, 150, 40), "QUIT")) Quit();
    }

    void ConfirmSaveAction(string saveId, SaveAction action)
    {
        pendingSaveId = saveId;
        pendingSaveAction = action;
        returnScreen = MenuScreen.Saves;
        Open(MenuScreen.ConfirmSaveAction);
    }

    void DrawConfirmSaveAction()
    {
        bool isNewGame = pendingSaveAction == SaveAction.NewGame;
        Rect box = Panel(420f, 235f);
        Label(isNewGame ? "START A NEW GAME?" : "DELETE THIS SAVE?",
            new Rect(box.x, box.y + 28, box.width, 34), 21, new Color(1f, .68f, .4f));
        string message = isNewGame
            ? "This will begin a new game. Your current active save will be saved first."
            : "This save will be permanently removed.";
        Label(message, new Rect(box.x + 28, box.y + 78, box.width - 56, 46), 12, Color.white);
        if (GUI.Button(new Rect(box.x + 30, box.y + 166, 160, 40), "CANCEL"))
        {
            pendingSaveAction = SaveAction.None;
            Open(MenuScreen.Saves);
        }
        if (GUI.Button(new Rect(box.x + 230, box.y + 166, 160, 40), isNewGame ? "START NEW" : "DELETE"))
        {
            if (pendingSaveAction == SaveAction.NewGame)
            {
                SaveActiveGame();
                StartNewGame(pendingSaveId);
                Resume();
            }
            else if (pendingSaveAction == SaveAction.Delete)
            {
                DeleteSave(pendingSaveId);
                Open(MenuScreen.Saves);
            }
            pendingSaveAction = SaveAction.None;
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

    void StartNewGame(string saveId)
    {
        ClearGameState();
        GameState.Replace(GameState.FromLegacyPrefs());
        activeSaveId = saveId;
        ChoppableTree.StartNewSave();
        ItemInventory.EnsureStarterTools();
        PlayerPrefs.SetString(ActiveSaveKey, activeSaveId);
        PlayerPrefs.Save();
        SceneManager.LoadScene(GameSceneCatalog.Name(GameScene.TownHub));
        SaveActiveGame();
    }

    void Quit()
    {
        SaveActiveGame();
#if UNITY_EDITOR
        Debug.Log("QUIT GAME requested. Application.Quit is ignored in the Unity Editor.");
#else
        Application.Quit();
#endif
    }

    void SaveActiveGame()
    {
        if (string.IsNullOrEmpty(activeSaveId)) return;
        GameState.InstallFromRuntime();
        ItemInventory.EnsureStarterAxe();
        GameState.Active.gold = ItemInventory.GetTotal(InventoryItemId.Gold);
        GameState.Active.playerItems = ItemInventory.ReadSlots(ItemInventory.Container.PlayerInventory);
        GameState.Active.chestItems = ItemInventory.ReadSlots(ItemInventory.Container.HomeChest);
        var data = new SaveData
        {
            exists = true,
            saveId = activeSaveId,
            displayName = string.IsNullOrEmpty(activeSaveId) ? "SAVE" : $"SAVE {activeSaveId.Substring(0, 6).ToUpperInvariant()}",
            savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            gold = ItemInventory.GetTotal(InventoryItemId.Gold),
            health = GameState.Active.health,
            energy = GameState.Active.energy,
            energySaved = true,
            wood = ItemInventory.GetTotal(InventoryItemId.Wood),
            itemsSaved = true,
            playerItems = ItemInventory.ReadSlots(ItemInventory.Container.PlayerInventory),
            chestItems = ItemInventory.ReadSlots(ItemInventory.Container.HomeChest),
            completedRuns = GameState.Active.completedRuns,
            villageMinutes = (float)GameState.Active.villageMinutes,
            injured = GameState.Active.injured ? 1 : 0,
            meleeUpgrade = GameState.Active.meleeUpgrade,
            infirmaryLevel = GameState.Active.infirmaryLevel,
            lastSeed = GameState.Active.lastSeed,
            expeditionRunIdentity = GameState.Active.expeditionRunIdentity,
            gameState = GameState.Active,
            playerGold = null,
            chestGold = null
        };
        try { WriteSave(data); }
        catch (Exception exception) { Debug.LogError($"Could not save game {activeSaveId}: {exception.Message}"); }
    }

    void RestoreSave(string saveId)
    {
        SaveData data = LoadSave(saveId);
        if (!data.exists) return;
        GameState restoredState = data.gameState ?? new GameState
        {
            health = data.health,
            energy = data.energySaved ? data.energy : ExpeditionPlayerEnergy.DefaultStartingEnergy,
            injured = data.injured != 0,
            completedRuns = data.completedRuns,
            meleeUpgrade = data.meleeUpgrade,
            infirmaryLevel = data.infirmaryLevel,
            lastSeed = data.lastSeed,
            villageMinutes = data.villageMinutes,
            expeditionRunIdentity = data.expeditionRunIdentity ?? ""
        };
        ExpeditionRunProgression.RecoverCompletedTutorial(restoredState);
        ClearGameState();
        GameState.Replace(restoredState);
        GameState.Active.gold = data.gold;
        GameState.Active.pendingSecuredGold = data.gameState != null ? data.gameState.pendingSecuredGold : 0;
        GameState.Active.playerItems = data.itemsSaved ? data.playerItems : GameState.Active.playerItems;
        GameState.Active.chestItems = data.itemsSaved ? data.chestItems : GameState.Active.chestItems;
        GameState.Active.ApplyRuntimeState();
        PlayerPrefs.SetInt(TownHubController.GoldKey, data.gold);
        PlayerPrefs.SetInt(ExpeditionPlayerHealth.HealthKey, GameState.Active.health);
        // Older save-slot JSON predates energy. Treat those saves as fully
        // rested rather than restoring JsonUtility's missing-field default of 0.
        PlayerPrefs.SetFloat(ExpeditionPlayerEnergy.EnergyKey, GameState.Active.energy);
        if (data.itemsSaved)
        {
            ItemInventory.WriteSlots(ItemInventory.Container.PlayerInventory, data.playerItems);
            ItemInventory.WriteSlots(ItemInventory.Container.HomeChest, data.chestItems);
        }
        else if (data.woodStacksSaved)
        {
            RestoreLegacyStacks(ItemInventory.Container.PlayerInventory, InventoryItemId.Wood, data.playerWood);
            RestoreLegacyStacks(ItemInventory.Container.HomeChest, InventoryItemId.Wood, data.chestWood);
            if (data.playerWood == null && data.chestWood == null && data.wood > 0)
                ItemInventory.AddItem(ItemInventory.Container.PlayerInventory, InventoryItemId.Wood, data.wood);
        }
        else
        {
            // Older saves only had a total wood counter. Let the normal
            // migration place that total into the first available player slot.
            if (data.wood > 0)
                ItemInventory.AddItem(ItemInventory.Container.PlayerInventory,
                    InventoryItemId.Wood, data.wood);
        }
        if (!data.itemsSaved && data.toolsSaved)
            RestoreLegacyAxe(data.playerAxeSlot, data.chestAxeSlot);
        else if (!data.itemsSaved)
        {
            ItemInventory.EnsureStarterAxe();
        }
        ExpeditionRunIdentity.RestoreSerialized(GameState.Active.expeditionRunIdentity);
        VillageTime.Instance?.RestoreSavedTime(GameState.Active.villageMinutes);
        if (!data.itemsSaved)
        {
            RestoreLegacyStacks(ItemInventory.Container.PlayerInventory, InventoryItemId.Gold, data.playerGold);
            RestoreLegacyStacks(ItemInventory.Container.HomeChest, InventoryItemId.Gold, data.chestGold);
            if (data.playerGold == null && data.chestGold == null && data.gold > 0)
                ItemInventory.AddItem(ItemInventory.Container.PlayerInventory,
                    InventoryItemId.Gold, data.gold);
        }
        activeSaveId = saveId;
        PlayerPrefs.SetString(ActiveSaveKey, saveId);
        PlayerPrefs.Save();
        SceneManager.LoadScene(GameSceneCatalog.Name(GameScene.TownHub));
    }

    SaveData LoadSave(string saveId)
    {
        if (!IsSafeSaveId(saveId)) return default;
        string path = SavePath(saveId);
        if (!File.Exists(path)) return default;
        try { return JsonUtility.FromJson<SaveData>(File.ReadAllText(path)); }
        catch (Exception exception) { Debug.LogError($"Could not read save {saveId}: {exception.Message}"); return default; }
    }

    void DeleteSave(string saveId)
    {
        if (IsSafeSaveId(saveId))
        {
            string path = SavePath(saveId);
            if (File.Exists(path)) File.Delete(path);
        }
        if (activeSaveId == saveId) { activeSaveId = ""; PlayerPrefs.DeleteKey(ActiveSaveKey); }
        PlayerPrefs.Save();
    }

    List<SaveData> ListSaves()
    {
        var result = new List<SaveData>();
        string directory = SaveDirectoryPath();
        if (!Directory.Exists(directory)) return result;
        foreach (string path in Directory.GetFiles(directory, SaveFilePrefix + "*" + SaveFileSuffix))
        {
            string id = Path.GetFileNameWithoutExtension(path).Substring(SaveFilePrefix.Length);
            SaveData data = LoadSave(id);
            if (data.exists) result.Add(data);
        }
        result.Sort((a, b) => string.CompareOrdinal(b.savedAt, a.savedAt));
        return result;
    }

    static string SaveDirectoryPath() => Path.Combine(Application.persistentDataPath, SaveDirectoryName);
    static string SavePath(string saveId) => Path.Combine(SaveDirectoryPath(), SaveFilePrefix + saveId + SaveFileSuffix);
    static void WriteSave(SaveData data)
    {
        Directory.CreateDirectory(SaveDirectoryPath());
        if (!IsSafeSaveId(data.saveId)) throw new InvalidOperationException("Invalid save identifier.");
        string path = SavePath(data.saveId);
        string temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, JsonUtility.ToJson(data, true));
        if (File.Exists(path)) File.Replace(temporaryPath, path, null);
        else File.Move(temporaryPath, path);
    }

    void MigrateLegacySlotSaves()
    {
        // The previous build stored three JSON snapshots inside PlayerPrefs.
        // Import them once, then leave the old keys untouched so this migration
        // is recoverable if a player rolls back to an older build.
        for (int oldSlot = 0; oldSlot < 3; oldSlot++)
        {
            string legacyJson = PlayerPrefs.GetString(LegacySlotKeyPrefix + oldSlot, "");
            if (string.IsNullOrEmpty(legacyJson)) continue;
            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(legacyJson);
                if (!data.exists) continue;
                string id = string.IsNullOrEmpty(data.saveId) ? $"migrated-slot-{oldSlot + 1}" : data.saveId;
                data.saveId = id;
                if (string.IsNullOrEmpty(data.displayName)) data.displayName = $"SAVE {oldSlot + 1}";
                if (!IsSafeSaveId(id)) id = $"migrated-slot-{oldSlot + 1}";
                data.saveId = id;
                if (!File.Exists(SavePath(id))) WriteSave(data);
                if (string.IsNullOrEmpty(activeSaveId) && oldSlot == PlayerPrefs.GetInt("Session.ActiveSlot", -1))
                    activeSaveId = id;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not migrate legacy save {oldSlot}: {exception.Message}");
            }
        }
        if (!string.IsNullOrEmpty(activeSaveId))
        {
            PlayerPrefs.SetString(ActiveSaveKey, activeSaveId);
            PlayerPrefs.Save();
        }
    }

    static bool IsSafeSaveId(string saveId)
    {
        if (string.IsNullOrEmpty(saveId) || saveId.Length > 80) return false;
        foreach (char character in saveId)
            if (!(char.IsLetterOrDigit(character) || character == '-' || character == '_')) return false;
        return true;
    }
    static void RestoreLegacyStacks(ItemInventory.Container container, InventoryItemId item, int[] values)
    {
        if (values == null) return;
        int count = Mathf.Min(values.Length,
            container == ItemInventory.Container.PlayerInventory
                ? ItemInventory.PlayerSlotCount : ItemInventory.ChestSlotCount);
        for (int slot = 0; slot < count; slot++)
        {
            if (values[slot] <= 0) continue;
            if (ItemInventory.CanPlace(container, slot, item))
                ItemInventory.SetStack(container, slot, item, values[slot]);
            else
                ItemInventory.AddItem(container, item, values[slot]);
        }
    }

    static void RestoreLegacyAxe(int playerSlot, int chestSlot)
    {
        if (playerSlot >= 0 && ItemInventory.CanPlace(ItemInventory.Container.PlayerInventory, playerSlot, InventoryItemId.Axe))
            ItemInventory.SetStack(ItemInventory.Container.PlayerInventory, playerSlot, InventoryItemId.Axe, 1);
        if (chestSlot >= 0 && ItemInventory.CanPlace(ItemInventory.Container.HomeChest, chestSlot, InventoryItemId.Axe))
            ItemInventory.SetStack(ItemInventory.Container.HomeChest, chestSlot, InventoryItemId.Axe, 1);
        ItemInventory.EnsureStarterAxe();
    }
    static bool HasLegacyGameState()
        => PlayerPrefs.HasKey(TownHubController.GoldKey)
            || PlayerPrefs.HasKey(ExpeditionPlayerHealth.HealthKey)
            || PlayerPrefs.HasKey("Village.TotalMinutes")
            || PlayerPrefs.HasKey(ExpeditionRunProgression.CompletedRunsKey);
    static void ClearGameState()
    {
        VillageTime.ResetSavedClock();
        ItemInventory.ResetSavedState();
        CurrentExpeditionLoot.ResetSavedState();
        string[] keys = { TownHubController.GoldKey, TownHubController.PendingSecuredGoldKey, ExpeditionPlayerHealth.HealthKey,
            ExpeditionPlayerEnergy.EnergyKey,
            ExpeditionPlayerHealth.InjuryKey, PlayerProgression.ReinforcedMeleeKey, ExpeditionRunProgression.CompletedRunsKey,
            ExpeditionRunProgression.PendingThreatIncreaseKey, ExpeditionSeedManager.LastSeedKey, "Expedition.PendingSeed", "Expedition.HasPendingSeed",
            "Expedition.PendingSeedSource", "Expedition.PendingDailyDate",
            "Expedition.PendingRunResult", "Expedition.ResultSuccess", "Expedition.ResultSecuredGold", "Expedition.ResultLostGold", "Expedition.ResultLostItemCount", "Expedition.ResultHealthUnits", "Expedition.ResultNextThreat" };
        foreach (string key in keys) PlayerPrefs.DeleteKey(key);
        PlayerPrefs.DeleteKey(TownUpgradeBuilding.ProgressKey("infirmary"));
        ExpeditionRunIdentity.Clear();
        PlayerPrefs.Save();
    }
}
