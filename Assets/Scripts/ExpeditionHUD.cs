using UnityEngine;
using UnityEngine.SceneManagement;

public class ExpeditionHUD : MonoBehaviour
{
    public const int QuickbarSlotCount = 4;
    // IMGUI coordinates are already screen pixels, so keep a small, consistent safe margin.
    const float QuickbarBottomMargin = 10f;
    const float HeartsToQuickbarGap = 10f;
    const float QuickbarRightOffset = 10f;
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

    static Texture2D quickbarArt;

    public ExpeditionPlayerHealth health;
    public int CarriedLoot { get; private set; }

    string statusMessage = "";
    float statusUntil;
    float goldCountBounceStartedAt = float.NegativeInfinity;

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

    public void AddLoot(int amount)
    {
        if (amount <= 0) return;
        CarriedLoot += amount;
        statusMessage = $"+{amount} Gold";
        statusUntil = Time.time + 1.4f;
        goldCountBounceStartedAt = Time.unscaledTime;
    }

    public int SecureLoot()
    {
        int secured = CarriedLoot;
        CarriedLoot = 0;
        if (secured > 0) TownHubController.AddSecuredGold(secured);
        return secured;
    }

    public void LoseLoot() => CarriedLoot = 0;

    public void GetPlayerInventoryGold(out int amount, out int slot)
    {
        int securedGold = TownHubController.GetGoldBalance();
        bool storedInPlayerInventory =
            GoldInventoryLocation.CurrentContainer == GoldInventoryLocation.Container.PlayerInventory;
        amount = storedInPlayerInventory ? securedGold + CarriedLoot : CarriedLoot;
        slot = storedInPlayerInventory ? GoldInventoryLocation.CurrentSlot : 0;
    }

    void OnGUI()
    {
        int maxHearts = health ? health.maxHearts : ExpeditionPlayerHealth.DefaultMaxHearts;
        int healthUnits = health
            ? health.CurrentHealth
            : Mathf.Clamp(PlayerPrefs.GetInt(ExpeditionPlayerHealth.HealthKey,
                ExpeditionPlayerHealth.DefaultMaxHealthUnits), 0, ExpeditionPlayerHealth.DefaultMaxHealthUnits);

        GetPlayerInventoryGold(out int displayedGold, out int displayedSlot);
        float bounceProgress = Mathf.Clamp01((Time.unscaledTime - goldCountBounceStartedAt) /
            GoldCountBounceDuration);
        float countBounce = Time.unscaledTime - goldCountBounceStartedAt < GoldCountBounceDuration
            ? Mathf.Sin(bounceProgress * Mathf.PI)
            : 0f;
        DrawQuickbar(displayedGold, displayedSlot, countBounce);

        Rect quickbar = GetQuickbarRect();
        float heartsWidth = HealthHeartGUI.GetWidth(maxHearts);
        float heartsX = quickbar.center.x - heartsWidth * .5f;
        float heartsY = quickbar.y + QuickbarArtworkTop * GetPixelScale()
            - HealthHeartGUI.HeartHeight - HeartsToQuickbarGap;
        HealthHeartGUI.Draw(healthUnits, maxHearts, heartsX, heartsY);

        if (Time.time < statusUntil)
        {
            var oldColor = GUI.color;
            var noticeStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = 14,
                fontStyle = FontStyle.Normal,
                wordWrap = true,
                normal = { textColor = new Color(.92f, .9f, .82f, .78f) }
            };
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width / 2f - 280f, 10f, 560f, 42f),
                statusMessage, noticeStyle);
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
        return new Rect(Mathf.Round((Screen.width - width) * .5f + QuickbarRightOffset * scale),
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

    public static void DrawQuickbar(int goldAmount, int goldSlot, float countBounce = 0f)
    {
        var oldColor = GUI.color;
        Rect panel = GetQuickbarRect();
        Texture2D art = GetQuickbarArt();
        GUI.color = Color.white;
        GUI.DrawTexture(panel, art ? art : Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
        if (goldAmount > 0 && goldSlot >= 0 && goldSlot < QuickbarSlotCount)
        {
            // Match the full inventory exactly: draw within the cell's inset content box,
            // rather than using the raw quickbar cell bounds.
            Rect slot = GetQuickbarSlotRect(goldSlot);
            float inset = Mathf.Max(2f, panel.width / 110f);
            Rect content = new Rect(slot.x + inset, slot.y + inset,
                slot.width - inset * 2f, slot.height - inset * 2f);
            DrawGoldStack(content, goldAmount, content.width / 50f, countBounce);
        }
        GUI.color = oldColor;
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
