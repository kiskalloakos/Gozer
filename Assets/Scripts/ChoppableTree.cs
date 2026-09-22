using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Tree resource node that can only be damaged while the player owns the axe.</summary>
[RequireComponent(typeof(Collider2D))]
public sealed class ChoppableTree : MonoBehaviour
{
    const string RegrowDayPrefix = "Tree.RegrowDay.";
    const string TownScene = "TownHub";
    const string LogTexturePath = "UI/cut out tree";

    [Min(1)] public int hitsToChop = 3;
    [Min(1)] public int woodYield = 1;

    SpriteRenderer visual;
    Collider2D treeCollider;
    Sprite originalSprite;
    Sprite logSprite;
    int remainingHits;
    bool chopped;

    void Awake()
    {
        visual = GetComponentInChildren<SpriteRenderer>();
        treeCollider = GetComponent<Collider2D>();
        originalSprite = visual ? visual.sprite : null;
        logSprite = CreateLogSprite();
        ResetTree();
    }

    void OnEnable()
    {
        if (Application.isPlaying && treeCollider) RefreshSavedState();
    }

    void Start()
    {
        if (Application.isPlaying) RefreshSavedState();
    }

    void Update()
    {
        if (!chopped || !TracksRegrowth || !VillageTime.Instance) return;
        if (VillageTime.Instance.Day >= SavedRegrowDay()) Regrow();
    }

    public bool TryChop(InventoryItemId equippedTool)
    {
        if (equippedTool != InventoryItemId.Axe || chopped) return false;

        remainingHits = Mathf.Max(0, remainingHits - 1);
        if (remainingHits > 0) return true;

        chopped = true;
        if (treeCollider) treeCollider.enabled = false;
        if (visual)
        {
            visual.sprite = logSprite ? logSprite : originalSprite;
            visual.enabled = true;
        }
        WoodPickup.Spawn(transform.position + Vector3.up * .2f, woodYield);
        if (TracksRegrowth)
        {
            int currentDay = VillageTime.Instance ? VillageTime.Instance.Day : 1;
            PlayerPrefs.SetInt(RegrowthKey(), currentDay + 1);
            PlayerPrefs.Save();
        }
        return true;
    }

    /// <summary>Resets a pooled expedition tree when the generated forest reuses it.</summary>
    public void ResetForGeneration()
    {
        if (!Application.isPlaying) return;
        ResetTree();
    }

    void RefreshSavedState()
    {
        if (!TracksRegrowth)
        {
            ResetTree();
            return;
        }

        int regrowDay = SavedRegrowDay();
        if (regrowDay <= 0)
        {
            ResetTree();
            return;
        }

        if (VillageTime.Instance && VillageTime.Instance.Day >= regrowDay)
        {
            Regrow();
            return;
        }

        SetChoppedVisual();
    }

    void ResetTree()
    {
        chopped = false;
        remainingHits = Mathf.Max(1, hitsToChop);
        if (treeCollider) treeCollider.enabled = true;
        if (visual)
        {
            visual.sprite = originalSprite;
            visual.enabled = true;
        }
    }

    void SetChoppedVisual()
    {
        chopped = true;
        remainingHits = 0;
        if (treeCollider) treeCollider.enabled = false;
        if (visual)
        {
            visual.sprite = logSprite ? logSprite : originalSprite;
            visual.enabled = true;
        }
    }

    void Regrow()
    {
        PlayerPrefs.DeleteKey(RegrowthKey());
        PlayerPrefs.Save();
        ResetTree();
    }

    bool TracksRegrowth
        => SceneManager.GetActiveScene().name == TownScene;

    string RegrowthKey()
        => $"{RegrowDayPrefix}{SceneManager.GetActiveScene().name}.{name}."
            + $"{Mathf.RoundToInt(transform.position.x * 100f)}."
            + Mathf.RoundToInt(transform.position.y * 100f);

    int SavedRegrowDay()
        => PlayerPrefs.GetInt(RegrowthKey(), 0);

    Sprite CreateLogSprite()
    {
        Texture2D texture = Resources.Load<Texture2D>(LogTexturePath);
        if (!texture) return null;
        texture.filterMode = FilterMode.Point;
        return Sprite.Create(texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(.5f, 0f), PixelArtStandard.PixelsPerUnit);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InstallBootstrap()
    {
        SceneManager.sceneLoaded -= InstallForScene;
        SceneManager.sceneLoaded += InstallForScene;
        InstallForScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void InstallForScene(Scene scene, LoadSceneMode mode)
    {
        foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (!transform.name.ToLowerInvariant().Contains("tree")) continue;
            if (!transform.GetComponent<SpriteRenderer>() && !transform.GetComponentInChildren<SpriteRenderer>()) continue;
            if (!transform.GetComponent<Collider2D>()) continue;
            if (!transform.GetComponent<ChoppableTree>()) transform.gameObject.AddComponent<ChoppableTree>();
        }
    }

    void OnDestroy()
    {
        if (logSprite) Destroy(logSprite);
    }
}
