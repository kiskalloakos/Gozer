using UnityEngine;
using UnityEngine.SceneManagement;

public class ExpeditionHUD : MonoBehaviour
{
    public const int QuickbarSlotCount = 4;
    // IMGUI coordinates are already screen pixels, so keep a small, consistent safe margin.
    const float QuickbarBottomMargin = 10f;
    const float HeartsToQuickbarGap = 10f;
    const float QuickbarArtworkTop = 13f;
    const float QuickbarArtworkBottom = 37f;
    const float GoldCountBounceDuration = .38f;
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

    public ExpeditionPlayerHealth health;
    public int ActiveQuickbarSlot { get; private set; }
    readonly int[] carriedLootBySlot = new int[GoldInventoryLocation.PlayerSlotCount];
    public int CarriedLoot
    {
        get
        {
            int total = 0;
            foreach (int amount in carriedLootBySlot) total += amount;
            return total;
        }
    }

    string statusMessage = "";
    float statusUntil;
    float goldCountBounceStartedAt = float.NegativeInfinity;
    int goldCountBounceSlot = -1;

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
        if (!GetComponent<PlayerInventoryUI>()) gameObject.AddComponent<PlayerInventoryUI>();
    }

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

    public void AddLoot(int amount)
    {
        if (amount <= 0) return;
        int slot = FindCarriedLootSlot();
        carriedLootBySlot[slot] += amount;
        statusMessage = $"+{amount} Gold";
        statusUntil = Time.time + 1.4f;
        goldCountBounceStartedAt = Time.unscaledTime;
        goldCountBounceSlot = slot;
    }

    public int SecureLoot()
    {
        int secured = CarriedLoot;
        for (int slot = 0; slot < carriedLootBySlot.Length; slot++)
        {
            int amount = carriedLootBySlot[slot];
            if (amount <= 0) continue;
            GoldInventoryLocation.AddAmount(GoldInventoryLocation.Container.PlayerInventory, slot, amount);
            carriedLootBySlot[slot] = 0;
        }
        if (secured > 0)
        {
            PlayerPrefs.SetInt(TownHubController.PendingSecuredGoldKey,
                PlayerPrefs.GetInt(TownHubController.PendingSecuredGoldKey, 0) + secured);
            PlayerPrefs.Save();
        }
        return secured;
    }

    public void LoseLoot() => System.Array.Clear(carriedLootBySlot, 0, carriedLootBySlot.Length);

    public int GetCarriedLootAtSlot(int slot)
        => slot >= 0 && slot < carriedLootBySlot.Length ? carriedLootBySlot[slot] : 0;

    public void SetCarriedLootAtSlot(int slot, int amount)
    {
        if (slot < 0 || slot >= carriedLootBySlot.Length) return;
        carriedLootBySlot[slot] = Mathf.Max(0, amount);
    }

    void OnGUI()
    {
        int maxHearts = health ? health.maxHearts : ExpeditionPlayerHealth.DefaultMaxHearts;
        int healthUnits = health
            ? health.CurrentHealth
            : Mathf.Clamp(PlayerPrefs.GetInt(ExpeditionPlayerHealth.HealthKey,
                ExpeditionPlayerHealth.DefaultMaxHealthUnits), 0, ExpeditionPlayerHealth.DefaultMaxHealthUnits);

        float bounceProgress = Mathf.Clamp01((Time.unscaledTime - goldCountBounceStartedAt) /
            GoldCountBounceDuration);
        float countBounce = Time.unscaledTime - goldCountBounceStartedAt < GoldCountBounceDuration
            ? Mathf.Sin(bounceProgress * Mathf.PI)
            : 0f;
        DrawQuickbar(this, goldCountBounceSlot, countBounce);

        Rect quickbar = GetQuickbarRect();
        float heartsWidth = HealthHeartGUI.GetWidth(maxHearts);
        float heartsX = quickbar.center.x - heartsWidth * .5f;
        float heartsY = quickbar.y + QuickbarArtworkTop * GetPixelScale()
            - HealthHeartGUI.HeartHeight - HeartsToQuickbarGap;
        HealthHeartGUI.Draw(healthUnits, maxHearts, heartsX, heartsY);

        if (Time.time < statusUntil)
        {
            var oldColor = GUI.color;
            GUI.color = Color.white;
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

    public static void DrawQuickbar(ExpeditionHUD hud, int bouncingSlot = -1, float countBounce = 0f)
    {
        var oldColor = GUI.color;
        Rect panel = GetQuickbarRect();
        Texture2D art = GetQuickbarArt();
        GUI.color = Color.white;
        GUI.DrawTexture(panel, art ? art : Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
        DrawQuickbarSlotSelection(panel, hud ? hud.ActiveQuickbarSlot : -1);
        for (int slotIndex = 0; slotIndex < QuickbarSlotCount; slotIndex++)
        {
            int goldAmount = GoldInventoryLocation.GetAmount(
                GoldInventoryLocation.Container.PlayerInventory, slotIndex)
                + (hud ? hud.GetCarriedLootAtSlot(slotIndex) : 0);
            if (goldAmount <= 0) continue;
            // Match the full inventory exactly: draw within the cell's inset content box,
            // rather than using the raw quickbar cell bounds.
            Rect slot = GetQuickbarSlotRect(slotIndex);
            float inset = Mathf.Max(2f, panel.width / 110f);
            Rect content = new Rect(slot.x + inset, slot.y + inset,
                slot.width - inset * 2f, slot.height - inset * 2f);
            DrawGoldStack(content, goldAmount, content.width / 50f,
                slotIndex == bouncingSlot ? countBounce : 0f);
        }
        DrawQuickbarSlotHotkeys(panel);
        GUI.color = oldColor;
    }

    public static void DrawQuickbarSlotSelection(Rect panel, int activeSlot)
    {
        if (activeSlot < 0 || activeSlot >= QuickbarSlotCount) return;
        Rect slot = GetQuickbarSlotRect(activeSlot);
        float thickness = Mathf.Max(1f, panel.width / 110f);
        GUI.color = new Color(1f, .8f, .22f, .22f);
        GUI.DrawTexture(slot, Texture2D.whiteTexture);
        GUI.color = new Color(1f, .87f, .42f, .95f);
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

    int FindCarriedLootSlot()
    {
        for (int slot = 0; slot < carriedLootBySlot.Length; slot++)
            if (carriedLootBySlot[slot] > 0) return slot;
        // Expedition pickups should merge into an existing secured player stack
        // before occupying a new inventory slot. Manual inventory actions can
        // still split or move stacks after collection.
        for (int slot = 0; slot < carriedLootBySlot.Length; slot++)
            if (GoldInventoryLocation.GetAmount(GoldInventoryLocation.Container.PlayerInventory, slot) > 0)
                return slot;
        for (int slot = 0; slot < carriedLootBySlot.Length; slot++)
            if (GoldInventoryLocation.GetAmount(GoldInventoryLocation.Container.PlayerInventory, slot) == 0)
                return slot;
        return 0;
    }

    public static void DrawGoldStack(Rect rect, int amount, float contentScale = 1f,
        float countBounce = 0f)
    {
        var oldColor = GUI.color;
        GUI.color = Color.white;
        GoldVisualAssets assets = GoldVisualAssets.Load();
        Sprite inventoryGold = assets ? assets.inventorySprite : null;
        if (inventoryGold)
        {
            Rect spriteRect = inventoryGold.rect;
            Texture2D texture = inventoryGold.texture;
            float availableSize = Mathf.Min(rect.width, rect.height);
            float sourceSize = Mathf.Max(spriteRect.width, spriteRect.height);
            // At normal game resolutions, use an integer texture scale so the icon stays
            // crisp. Leaving a little breathing room also makes its visual center clear.
            float textureScale = availableSize >= sourceSize
                ? Mathf.Max(1f, Mathf.Floor(availableSize / sourceSize))
                : availableSize / sourceSize;
            float tokenWidth = spriteRect.width * textureScale;
            float tokenHeight = spriteRect.height * textureScale;
            Rect tokenRect = new Rect(
                Mathf.Round(rect.center.x - tokenWidth * .5f),
                Mathf.Round(rect.center.y - tokenHeight * .5f),
                tokenWidth, tokenHeight);
            var textureCoords = new Rect(spriteRect.x / texture.width, spriteRect.y / texture.height,
                spriteRect.width / texture.width, spriteRect.height / texture.height);
            GUI.DrawTextureWithTexCoords(tokenRect, texture, textureCoords, true);
        }

        DrawGoldCount(rect, amount, countBounce);
        GUI.color = oldColor;
    }

    static void DrawGoldCount(Rect rect, int amount, float bounce)
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

        GUI.color = new Color(.12f, .055f, .025f, .94f);
        GUI.DrawTexture(badge, Texture2D.whiteTexture);

        float startX = badge.x + pixel;
        float startY = badge.y + pixel;
        GUI.color = new Color(1f, .94f, .68f);
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
        GUI.color = new Color(1f, .94f, .68f);
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
