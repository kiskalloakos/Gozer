using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Swaps to the pressed hand cursor while the primary mouse button is held.
/// The hotspot remains fixed so the pressed artwork's offset reads as movement.
/// </summary>
public sealed class CursorClickFeedback : MonoBehaviour
{
    public const float InteractionDelaySeconds = 0.08f;

    private const string PressedCursorResource = "UI/Cursors/StiffyCursorPressed";
    private const string HoverCursorResource = "UI/Cursors/StiffyCursorHover";
    private const float MinimumPressSeconds = 0.12f;
    private const int MinimumPressFrames = 3;
    private static readonly Vector2 Hotspot = new Vector2(7f, 13f);

    private static CursorClickFeedback instance;

    private Texture2D pressedCursor;
    private Texture2D hoverCursor;
    private bool showingPressedCursor;
    private bool showingInteractiveHover;
    private float releaseAfterTime;
    private int releaseAfterFrame;

    public static void Pulse()
    {
        if (instance) instance.BeginPress();
    }

    public static void SetInteractiveHover(bool hovering)
    {
        if (instance) instance.SetHovering(hovering);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance) return;

        var cursorObject = new GameObject(nameof(CursorClickFeedback));
        cursorObject.AddComponent<CursorClickFeedback>();
        DontDestroyOnLoad(cursorObject);
    }

    private void Awake()
    {
        if (instance && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        pressedCursor = Resources.Load<Texture2D>(PressedCursorResource);
        hoverCursor = Resources.Load<Texture2D>(HoverCursorResource);
        if (!pressedCursor)
            Debug.LogWarning($"Pressed cursor not found at Resources/{PressedCursorResource}.");
        if (!hoverCursor)
            Debug.LogWarning($"Hover cursor not found at Resources/{HoverCursorResource}.");

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            BeginPress();

        if (Input.GetMouseButton(0))
        {
            SetPressed(true);
            return;
        }

        if (showingPressedCursor &&
            Time.unscaledTime >= releaseAfterTime &&
            Time.frameCount >= releaseAfterFrame)
            SetPressed(false);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            showingInteractiveHover = false;
            SetPressed(false);
        }
    }

    private void OnDisable()
    {
        showingPressedCursor = false;
        showingInteractiveHover = false;
        ApplyCursor();
    }

    private void BeginPress()
    {
        releaseAfterTime = Time.unscaledTime + MinimumPressSeconds;
        releaseAfterFrame = Time.frameCount + MinimumPressFrames;
        SetPressed(true);
    }

    private void SetPressed(bool pressed)
    {
        pressed = pressed && pressedCursor != null;
        if (showingPressedCursor == pressed) return;

        showingPressedCursor = pressed;
        ApplyCursor();
    }

    private void SetHovering(bool hovering)
    {
        hovering = hovering && hoverCursor != null && HoverCursorAllowed();
        if (showingInteractiveHover == hovering) return;
        showingInteractiveHover = hovering;
        ApplyCursor();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "ExpeditionField") SetHovering(false);
    }

    private static bool HoverCursorAllowed()
        => SceneManager.GetActiveScene().name != "ExpeditionField";

    private void ApplyCursor()
    {
        Texture2D cursor = showingPressedCursor
            ? pressedCursor
            : showingInteractiveHover ? hoverCursor : null;
        Cursor.SetCursor(cursor, cursor ? Hotspot : Vector2.zero, CursorMode.Auto);
    }
}
