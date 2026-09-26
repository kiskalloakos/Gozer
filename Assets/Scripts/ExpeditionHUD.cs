using UnityEngine;
using UnityEngine.SceneManagement;

public class ExpeditionHUD : MonoBehaviour
{
    public const int QuickbarSlotCount = 4;
    const string PickupSoundResourcePath = "Audio/UIClick_INTERFACE-Positive Click_HY_PC-003";
    // IMGUI coordinates are already screen pixels, so keep a small, consistent safe margin.
    const float QuickbarBottomMargin = 10f;
    const float HeartsToQuickbarGap = 10f;
    const float HeartsToEnergyGap = 9f;
    const float QuickbarArtworkTop = 13f;
    const float QuickbarArtworkBottom = 37f;
    const float StackCountBounceDuration = .38f;
    static readonly float[] QuickbarSourceColumnCenters = { 29f, 47f, 64f, 81f };
    static readonly string[] GoldCountGlyphs =
    {
        "111101101101111", // 0
        "010110010010111", // 1
        "111001111100111", // 2
        "111001111001111", // 3
        "101101111001001", // 4
        "111100111001111", // 5
        "111100111101111", // 6
        "111001001001001", // 7
        "111101111101111", // 8
        "111101111001111"  // 9
    };
    static readonly string[] PixelLetterGlyphs =
    {
        "010101111101101", "110101110101110", "011100100100011", "110101101101110",
        "111100110100111", "111100110100100", "011100101101011", "101101111101101",
        "111010010010111", "001001001101010", "101101110101101", "100100100100111",
        "101111111101101", "110111111101101", "010101101101010", "110101110100100",
        "010101101111011", "110101110101101", "011100010001110", "111010010010010",
        "101101101101010", "101101101101010", "101101111111101", "101101010101101",
        "101101010010010", "111001010100111"
    };

    static Texture2D quickbarArt;
    static Texture2D axeArt;
    static Texture2D woodenPickaxeArt;
    static Texture2D woodenSwordArt;
    static Texture2D woodenShovelArt;
    static Texture2D woodArt;
    static ExpeditionHUD cachedHud;
    static Scene cachedScene;
    static AudioClip pickupSound;
    readonly float[] stackCountBounceStartedAt = CreateStackCountBounceTimers();

    public ExpeditionPlayerHealth health;
    public ExpeditionPlayerEnergy energy;
    public int ActiveQuickbarSlot { get; private set; }
    public InventoryItemId ActiveQuickbarItem
        => ActiveQuickbarSlot >= 0 && ActiveQuickbarSlot < ItemInventory.PlayerSlotCount
            ? ItemInventory.GetStack(ItemInventory.Container.PlayerInventory, ActiveQuickbarSlot).item
            : InventoryItemId.Empty;
    public bool IsAxeEquipped
        => ActiveQuickbarItem == InventoryItemId.Axe;
    public bool IsSwordEquipped
        => ActiveQuickbarItem == InventoryItemId.Sword;
    public int CarriedLoot => CurrentExpeditionLoot.Total;

    string statusMessage = "";
    float statusUntil;

    static float[] CreateStackCountBounceTimers()
    {
        var timers = new float[ItemInventory.PlayerSlotCount];
        for (int slot = 0; slot < timers.Length; slot++)
            timers[slot] = float.NegativeInfinity;
        return timers;
    }

    // The HUD is scene-owned, but its invariant is game-wide: every loaded
    // gameplay scene must have one usable HUD. A persistent watchdog repairs
    // accidental disables and timing gaps around scene transitions.
    sealed class HUDWatchdog : MonoBehaviour
    {
        void Update()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (cachedScene == scene && cachedHud && cachedHud.enabled
                && cachedHud.gameObject.activeInHierarchy) return;
            EnsureHUD(scene, LoadSceneMode.Single);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InstallWatchdog()
    {
        var existing = FindAnyObjectByType<HUDWatchdog>();
        if (existing) return;

        var watchdog = new GameObject("Player HUD Watchdog");
        watchdog.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(watchdog);
        watchdog.AddComponent<HUDWatchdog>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InstallForCurrentAndFutureScenes()
    {
        SceneManager.sceneLoaded -= EnsureHUD;
        SceneManager.sceneLoaded += EnsureHUD;
        EnsureHUD(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void EnsureHUD(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        cachedScene = scene;
        cachedHud = null;

        ExpeditionHUD firstHud = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var huds = root.GetComponentsInChildren<ExpeditionHUD>(true);
            foreach (var hud in huds)
            {
                if (!firstHud) firstHud = hud;
                if (!hud.enabled) hud.enabled = true;
                if (!hud.gameObject.activeSelf) hud.gameObject.SetActive(true);
                if (!root.activeSelf) root.SetActive(true);
            }
        }
        if (firstHud) { cachedHud = firstHud; return; }

        foreach (var root in scene.GetRootGameObjects())
        {
            var player = root.GetComponentInChildren<TownPlayerController>(true);
            if (!player) continue;
            var hud = player.GetComponent<ExpeditionHUD>();
            if (!hud) hud = player.gameObject.AddComponent<ExpeditionHUD>();
            hud.enabled = true;
            player.gameObject.SetActive(true);
            cachedHud = hud;
            return;
        }

        var hudObject = new GameObject("Player HUD");
        if (hudObject.scene != scene) SceneManager.MoveGameObjectToScene(hudObject, scene);
        var fallbackHud = hudObject.AddComponent<ExpeditionHUD>();
        fallbackHud.enabled = true;
        cachedHud = fallbackHud;
    }

    void Awake()
    {
        if (!health) health = GetComponent<ExpeditionPlayerHealth>();
        if (!health) health = FindAnyObjectByType<ExpeditionPlayerHealth>();
        if (!energy) energy = GetComponent<ExpeditionPlayerEnergy>();
        if (!energy && health) energy = health.GetComponent<ExpeditionPlayerEnergy>();
        var playerController = GetComponent<TownPlayerController>()
            ? GetComponent<TownPlayerController>()
            : FindAnyObjectByType<TownPlayerController>();
        if (!energy)
        {
            if (playerController)
            {
                energy = playerController.GetComponent<ExpeditionPlayerEnergy>();
                if (!energy) energy = playerController.gameObject.AddComponent<ExpeditionPlayerEnergy>();
            }
        }
        if (playerController && !playerController.GetComponent<ToolHeldVisual>())
            playerController.gameObject.AddComponent<ToolHeldVisual>();
        if (!GetComponent<PlayerInventoryUI>()) gameObject.AddComponent<PlayerInventoryUI>();
    }

    void OnEnable() => ItemInventory.PlayerItemPickedUp += PlayStackCountBounce;

    void OnDisable() => ItemInventory.PlayerItemPickedUp -= PlayStackCountBounce;

    void Update()
    {
        if (GameSessionFlow.IsBlockingGameplay) return;

        for (int slot = 0; slot < QuickbarSlotCount; slot++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + slot))
                || Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad1 + slot)))
            {
                ActiveQuickbarSlot = slot;
                return;
            }
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) < Mathf.Epsilon) return;
        int direction = scroll > 0f ? -1 : 1;
        ActiveQuickbarSlot = (ActiveQuickbarSlot + direction + QuickbarSlotCount) % QuickbarSlotCount;
    }

    public bool AddLoot(int amount)
    {
        if (amount <= 0) return false;
        if (!CurrentExpeditionLoot.Add(amount)) return false;
        PlayPickupSound();
        statusMessage = $"+{amount} Gold";
        statusUntil = Time.time + 1.4f;
        return true;
    }

    public void PlayStackCountBounce(int slot)
    {
        if (slot < 0 || slot >= ItemInventory.PlayerSlotCount) return;
        stackCountBounceStartedAt[slot] = Time.unscaledTime;
        PlayPickupSound();
    }

    void PlayPickupSound()
    {
        if (!pickupSound)
            pickupSound = Resources.Load<AudioClip>(PickupSoundResourcePath);
        if (pickupSound)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
    }

    public float GetStackCountBounce(int slot)
    {
        if (slot < 0 || slot >= stackCountBounceStartedAt.Length) return 0f;
        float elapsed = Time.unscaledTime - stackCountBounceStartedAt[slot];
        if (elapsed < 0f || elapsed >= StackCountBounceDuration) return 0f;
        return Mathf.Sin(Mathf.Clamp01(elapsed / StackCountBounceDuration) * Mathf.PI);
    }

    public void ShowStatus(string message, float seconds = 1.4f)
    {
        statusMessage = message;
        statusUntil = Time.time + seconds;
    }

    public int SecureLoot()
    {
        return CurrentExpeditionLoot.Secure();
    }

    public void LoseLoot() => CurrentExpeditionLoot.Lose();

    void OnGUI()
    {
        int maxHearts = health ? health.maxHearts : ExpeditionPlayerHealth.DefaultMaxHearts;
        int healthUnits = health
            ? health.CurrentHealth
            : Mathf.Clamp(GameState.Active != null ? GameState.Active.health : PlayerPrefs.GetInt(ExpeditionPlayerHealth.HealthKey,
                ExpeditionPlayerHealth.DefaultMaxHealthUnits), 0, ExpeditionPlayerHealth.DefaultMaxHealthUnits);
        int maxEnergy = energy ? Mathf.CeilToInt(energy.MaxEnergy) : Mathf.CeilToInt(ExpeditionPlayerEnergy.DefaultMaxEnergy);
        float currentEnergy = energy
            ? energy.CurrentEnergy
            : Mathf.Clamp(GameState.Active != null ? GameState.Active.energy : PlayerPrefs.GetFloat(ExpeditionPlayerEnergy.EnergyKey,
                ExpeditionPlayerEnergy.DefaultStartingEnergy), 0f, maxEnergy);

        DrawQuickbar(this);

        Rect quickbar = GetQuickbarRect();
        float heartsWidth = HealthHeartGUI.GetWidth(maxHearts);
        float energyWidth = EnergyBoltGUI.GetWidth(maxEnergy);
        float statusWidth = heartsWidth + HeartsToEnergyGap + energyWidth;
        float heartsX = quickbar.center.x - statusWidth * .5f;
        float heartsY = quickbar.y + QuickbarArtworkTop * GetPixelScale()
            - HealthHeartGUI.HeartHeight - HeartsToQuickbarGap;
        HealthHeartGUI.Draw(healthUnits, maxHearts, heartsX, heartsY);
        EnergyBoltGUI.Draw(currentEnergy, maxEnergy,
            heartsX + heartsWidth + HeartsToEnergyGap, heartsY);

        if (Time.time < statusUntil)
        {
            var oldColor = GUI.color;
            GUI.color = HouseSceneFade.FadeColor(Color.white);
            DrawPixelTextCentered(statusMessage, 10f, 2f);
            GUI.color = oldColor;
        }
    }

    public static Rect GetQuickbarRect()
    {
        Texture2D art = GetQuickbarArt();
        float scale = GetPixelScale();
        float width = art ? art.width * scale : 110f * scale;
        float height = art ? art.height * scale : 50f * scale;
        // Position from the opaque artwork bounds. The source PNG has transparent
        // padding above and below the four visible slots.
        float y = Screen.height - Screen.safeArea.yMin
            - QuickbarArtworkBottom * scale - QuickbarBottomMargin;
        return new Rect(Mathf.Round((Screen.width - width) * .5f),
            Mathf.Round(y), width, height);
    }

    public static Rect GetQuickbarSlotRect(int slot)
    {
        Rect panel = GetQuickbarRect();
        float scale = panel.width / 110f;
        // bottom_inventory.png has hand-drawn, non-uniform column spacing.
        // Use each visual cell center rather than applying a shared pitch.
        const float sourceGridY = 16f;
        const float sourceCellSize = 17f;
        float cellSize = sourceCellSize * scale;
        return new Rect(panel.x + QuickbarSourceColumnCenters[slot] * scale - cellSize * .5f,
            panel.y + sourceGridY * scale, sourceCellSize * scale, sourceCellSize * scale);
    }

    public static void DrawQuickbar(ExpeditionHUD hud)
    {
        var oldColor = GUI.color;
        Rect panel = GetQuickbarRect();
        Texture2D art = GetQuickbarArt();
        GUI.color = HouseSceneFade.FadeColor(Color.white);
        GUI.DrawTexture(panel, art ? art : Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
        DrawQuickbarSlotSelection(panel, hud ? hud.ActiveQuickbarSlot : -1);
        for (int slotIndex = 0; slotIndex < QuickbarSlotCount; slotIndex++)
        {
            ItemStack stack = ItemInventory.GetStack(ItemInventory.Container.PlayerInventory, slotIndex);
            if (stack.item == InventoryItemId.Empty || stack.amount <= 0) continue;
            Rect slot = GetQuickbarSlotRect(slotIndex);
            float inset = Mathf.Max(2f, panel.width / 110f);
            Rect content = new Rect(slot.x + inset, slot.y + inset,
                slot.width - inset * 2f, slot.height - inset * 2f);
            DrawItemStack(content, stack.item, stack.amount, content.width / 50f,
                hud ? hud.GetStackCountBounce(slotIndex) : 0f);
        }
        DrawQuickbarSlotHotkeys(panel);
        GUI.color = oldColor;
    }

    public static void DrawQuickbarSlotSelection(Rect panel, int activeSlot)
    {
        if (activeSlot < 0 || activeSlot >= QuickbarSlotCount) return;
        Rect slot = GetQuickbarSlotRect(activeSlot);
        float thickness = Mathf.Max(1f, panel.width / 110f);
        GUI.color = HouseSceneFade.FadeColor(new Color(1f, .8f, .22f, .22f));
        GUI.DrawTexture(slot, Texture2D.whiteTexture);
        GUI.color = HouseSceneFade.FadeColor(new Color(1f, .87f, .42f, .95f));
        GUI.DrawTexture(new Rect(slot.x, slot.y, slot.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(slot.x, slot.yMax - thickness, slot.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(slot.x, slot.y, thickness, slot.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(slot.xMax - thickness, slot.y, thickness, slot.height), Texture2D.whiteTexture);
    }

    public static void DrawQuickbarSlotHotkeys(Rect panel)
    {
        float pixel = Mathf.Max(1f, Mathf.Floor(panel.width / 110f));
        for (int slot = 0; slot < QuickbarSlotCount; slot++)
        {
            Rect slotRect = GetQuickbarSlotRect(slot);
            DrawPixelTextAt((slot + 1).ToString(), slotRect.x + pixel * 3f,
                slotRect.y + pixel * 2f, pixel);
        }
    }

    public static void DrawItemStack(Rect rect, InventoryItemId item, int amount,
        float contentScale = 1f, float countBounce = 0f)
    {
        if (amount <= 0 || item == InventoryItemId.Empty || item == InventoryItemId.Gold) return;
        if (item == InventoryItemId.Wood)
        {
            DrawWoodStack(rect, amount, contentScale, countBounce);
            return;
        }

        DrawItemIcon(rect, item);
        if (!ItemInventory.IsTool(item)) DrawStackCount(rect, amount, countBounce);
    }

    public static void DrawToolIcon(Rect rect)
    {
        DrawItemIcon(rect, InventoryItemId.Axe);
    }

    public static void DrawItemIcon(Rect rect, InventoryItemId item)
    {
        Texture2D art = ToolArt(item);
        if (!art) return;
        art.filterMode = FilterMode.Point;

        var oldColor = GUI.color;
        GUI.color = HouseSceneFade.FadeColor(Color.white);
        float scale = Mathf.Min(rect.width / art.width, rect.height / art.height);
        Rect icon = new Rect(rect.center.x - art.width * scale * .5f,
            rect.center.y - art.height * scale * .5f,
            art.width * scale, art.height * scale);
        GUI.DrawTexture(icon, art, ScaleMode.StretchToFill, true);
        GUI.color = oldColor;
    }

    static Texture2D ToolArt(InventoryItemId item)
    {
        switch (item)
        {
            case InventoryItemId.Axe:
                if (!axeArt) axeArt = Resources.Load<Texture2D>("UI/wooden_axe");
                return axeArt;
            case InventoryItemId.Pickaxe:
                if (!woodenPickaxeArt) woodenPickaxeArt = Resources.Load<Texture2D>("UI/wooden_pickaxe");
                return woodenPickaxeArt;
            case InventoryItemId.Sword:
                if (!woodenSwordArt) woodenSwordArt = Resources.Load<Texture2D>("UI/wooden_sword");
                return woodenSwordArt;
            case InventoryItemId.Shovel:
                if (!woodenShovelArt) woodenShovelArt = Resources.Load<Texture2D>("UI/wooden_shovel");
                return woodenShovelArt;
            default:
                return null;
        }
    }

    public static void DrawWoodStack(Rect rect, int amount, float contentScale = 1f,
        float countBounce = 0f)
    {
        if (!woodArt) woodArt = Resources.Load<Texture2D>("UI/wood-onground");
        if (!woodArt) return;
        woodArt.filterMode = FilterMode.Point;

        var oldColor = GUI.color;
        GUI.color = HouseSceneFade.FadeColor(Color.white);
        float scale = Mathf.Min(rect.width / woodArt.width, rect.height / woodArt.height);
        Rect icon = new Rect(rect.center.x - woodArt.width * scale * .5f,
            rect.center.y - woodArt.height * scale * .5f,
            woodArt.width * scale, woodArt.height * scale);
        GUI.DrawTexture(icon, woodArt, ScaleMode.StretchToFill, true);
        DrawStackCount(rect, amount, countBounce);
        GUI.color = oldColor;
    }

    static void DrawStackCount(Rect rect, int amount, float bounce)
    {
        string count = Mathf.Max(0, amount).ToString();
        float pixel = Mathf.Max(1f, Mathf.Floor(rect.height / 13f));
        float glyphWidth = 3f * pixel;
        float countWidth = count.Length * glyphWidth + (count.Length - 1) * pixel;
        Rect badge = new Rect(
            Mathf.Round(rect.xMax - countWidth - pixel * 2f),
            Mathf.Round(rect.yMax - pixel * 7f - pixel * 2f * bounce),
            countWidth + pixel * 2f,
            pixel * 7f);

        GUI.color = HouseSceneFade.FadeColor(new Color(.12f, .055f, .025f, .94f));
        GUI.DrawTexture(badge, Texture2D.whiteTexture);

        float startX = badge.x + pixel;
        float startY = badge.y + pixel;
        GUI.color = HouseSceneFade.FadeColor(new Color(1f, .94f, .68f));
        for (int digitIndex = 0; digitIndex < count.Length; digitIndex++)
        {
            string glyph = GoldCountGlyphs[count[digitIndex] - '0'];
            float glyphX = startX + digitIndex * (glyphWidth + pixel);
            for (int row = 0; row < 5; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    if (glyph[row * 3 + column] != '1') continue;
                    GUI.DrawTexture(new Rect(glyphX + column * pixel, startY + row * pixel,
                        pixel, pixel), Texture2D.whiteTexture);
                }
            }
        }
    }

    // Shares the inventory count's exact 3x5 pixel digits, with matching clock letters.
    public static void DrawPixelTextCentered(string text, float y, float pixel)
        => DrawPixelTextAt(text, Screen.width * .5f, y, pixel);

    public static void DrawPixelTextAt(string text, float centerX, float y, float pixel)
    {
        Color oldColor = GUI.color;
        GUI.color = HouseSceneFade.FadeColor(new Color(1f, .94f, .68f));
        string[] words = text.ToUpperInvariant().Split(' ');
        string line = "";
        int lineNumber = 0;
        foreach (string word in words)
        {
            string candidate = string.IsNullOrEmpty(line) ? word : line + " " + word;
            if (candidate.Length > 52 && !string.IsNullOrEmpty(line))
            {
                DrawPixelLineAt(line, centerX, y + lineNumber * pixel * 7f, pixel);
                line = word;
                lineNumber++;
            }
            else line = candidate;
        }
        if (!string.IsNullOrEmpty(line)) DrawPixelLineAt(line, centerX, y + lineNumber * pixel * 7f, pixel);
        GUI.color = oldColor;
    }

    static void DrawPixelLineAt(string text, float centerX, float y, float pixel)
    {
        float x = Mathf.Round(centerX - (text.Length * 4 - 1) * pixel * .5f);
        foreach (char character in text)
        {
            string glyph = character >= '0' && character <= '9'
                ? GoldCountGlyphs[character - '0']
                : character >= 'A' && character <= 'Z'
                    ? PixelLetterGlyphs[character - 'A']
                    : character == ':' ? "000010000010000" : "000000000000000";
            for (int row = 0; row < 5; row++)
            for (int column = 0; column < 3; column++)
                if (glyph[row * 3 + column] == '1')
                    GUI.DrawTexture(new Rect(x + column * pixel, y + row * pixel, pixel, pixel), Texture2D.whiteTexture);
            x += 4 * pixel;
        }
    }

    public static GUIStyle CreateLabelStyle(int fontSize)
    {
        return new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(.78f, .82f, .88f) }
        };
    }

    public static float GetPixelScale() => Mathf.Max(1f, Mathf.Floor(Mathf.Min(
        Screen.width / (float)PixelArtStandard.ReferenceWidth,
        Screen.height / (float)PixelArtStandard.ReferenceHeight)));

    static Texture2D GetQuickbarArt()
    {
        if (!quickbarArt) quickbarArt = Resources.Load<Texture2D>("UI/bottom_inventory");
        return quickbarArt;
    }
}
