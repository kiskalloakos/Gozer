using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class HomeStorageChest : TownInteractable
{
    public SpriteRenderer chestRenderer;
    public Sprite[] openFrames;
    public Texture2D inventoryPopup;
    [Min(.01f)] public float secondsPerFrame = .1f;
    [Range(0f, 1f)] public float backdropOpacity = .72f;

    public static bool IsModalOpen { get; private set; }
    public override string Prompt => "Click to open storage chest";

    bool panelOpen;
    bool animating;
    bool draggingGold;
    TownPlayerController playerMovement;
    bool playerMovementWasEnabled;

    void Awake()
    {
        if (!chestRenderer) chestRenderer = GetComponent<SpriteRenderer>();
        if (chestRenderer && openFrames != null && openFrames.Length > 0)
            chestRenderer.sprite = openFrames[0];
    }

    public override void Interact()
    {
        if (animating || PlayerInventoryUI.IsOpen) return;
        if (panelOpen) Close();
        else StartCoroutine(Open());
    }

    IEnumerator Open()
    {
        animating = true;
        IsModalOpen = true;
        LockPlayer();
        if (chestRenderer && openFrames != null)
        {
            for (int i = 0; i < openFrames.Length; i++)
            {
                if (openFrames[i]) chestRenderer.sprite = openFrames[i];
                if (i < openFrames.Length - 1) yield return new WaitForSecondsRealtime(secondsPerFrame);
            }
        }
        panelOpen = true;
        animating = false;
    }

    public void Close()
    {
        if (!panelOpen || animating) return;
        draggingGold = false;
        panelOpen = false;
        StartCoroutine(CloseAndUnlock());
    }

    IEnumerator CloseAndUnlock()
    {
        animating = true;
        if (chestRenderer && openFrames != null)
        {
            for (int i = openFrames.Length - 2; i >= 0; i--)
            {
                yield return new WaitForSecondsRealtime(secondsPerFrame);
                if (openFrames[i]) chestRenderer.sprite = openFrames[i];
            }
        }
        animating = false;
        IsModalOpen = false;
        UnlockPlayer();
    }

    void Update()
    {
        if (panelOpen && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))) Close();
    }

    void OnGUI()
    {
        if (!panelOpen) return;

        GUI.depth = -1000;
        var previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, backdropOpacity);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

        float scale = PlayerInventoryUI.GetPanelScale();
        float panelWidth = 110f * scale;
        float gap = 16f * scale;
        Vector2 center = new Vector2(Screen.width * .5f, Screen.height * .5f);
        Rect playerPanel = PlayerInventoryUI.GetPanelRectAt(new Vector2(center.x - (panelWidth + gap) * .5f, center.y));
        Rect chestPanel = PlayerInventoryUI.GetPanelRectAt(new Vector2(center.x + (panelWidth + gap) * .5f, center.y));
        PlayerInventoryUI.DrawPanel(playerPanel, "INVENTORY");
        PlayerInventoryUI.DrawPanel(chestPanel, "CHEST");

        int gold = TownHubController.GetGoldBalance();
        GoldInventoryLocation.Container container = GoldInventoryLocation.CurrentContainer;
        int slot = GoldInventoryLocation.CurrentSlot;
        if (gold > 0 && !draggingGold)
        {
            if (container == GoldInventoryLocation.Container.PlayerInventory)
                PlayerInventoryUI.DrawGold(playerPanel, slot, gold);
            else
                PlayerInventoryUI.DrawGold(chestPanel, slot, gold);
        }

        if (gold > 0 && !draggingGold && Event.current != null)
        {
            Rect goldRect = container == GoldInventoryLocation.Container.PlayerInventory
                ? PlayerInventoryUI.GetSlotRect(playerPanel, slot)
                : PlayerInventoryUI.GetSlotRect(chestPanel, slot);
            if (goldRect.Contains(Event.current.mousePosition))
                PlayerInventoryUI.DrawItemTooltip(Event.current.mousePosition, "GOLD");
        }

        HandlePointer(playerPanel, chestPanel, gold, container, slot);
        if (draggingGold && Event.current != null)
        {
            var dragRect = new Rect(Event.current.mousePosition.x - 25f, Event.current.mousePosition.y - 25f, 50f, 50f);
            ExpeditionHUD.DrawGoldStack(dragRect, gold);
        }

        GUI.color = previousColor;
    }

    void HandlePointer(Rect playerPanel, Rect chestPanel, int gold,
        GoldInventoryLocation.Container container, int slot)
    {
        Event currentEvent = Event.current;
        if (currentEvent == null) return;

        Rect goldRect = container == GoldInventoryLocation.Container.PlayerInventory
            ? PlayerInventoryUI.GetSlotRect(playerPanel, slot)
            : PlayerInventoryUI.GetSlotRect(chestPanel, slot);
        CursorClickFeedback.SetInteractiveHover(gold > 0 && goldRect.Contains(currentEvent.mousePosition));
        if (currentEvent.button != 0) return;

        if (currentEvent.type == EventType.MouseDown)
        {
            if (gold > 0 && goldRect.Contains(currentEvent.mousePosition))
            {
                draggingGold = true;
                currentEvent.Use();
            }
            else if (playerPanel.Contains(currentEvent.mousePosition) || chestPanel.Contains(currentEvent.mousePosition))
            {
                currentEvent.Use();
            }
            else
            {
                Close();
                currentEvent.Use();
            }
        }
        else if (currentEvent.type == EventType.MouseDrag && draggingGold)
        {
            currentEvent.Use();
        }
        else if (currentEvent.type == EventType.MouseUp && draggingGold)
        {
            if (PlayerInventoryUI.TryGetSlotAt(currentEvent.mousePosition, playerPanel, out int playerSlot))
                GoldInventoryLocation.MoveTo(GoldInventoryLocation.Container.PlayerInventory, playerSlot);
            else if (PlayerInventoryUI.TryGetSlotAt(currentEvent.mousePosition, chestPanel, out int chestSlot))
                GoldInventoryLocation.MoveTo(GoldInventoryLocation.Container.HomeChest, chestSlot);
            draggingGold = false;
            currentEvent.Use();
        }
    }

    void LockPlayer()
    {
        playerMovement = FindAnyObjectByType<TownPlayerController>();
        if (!playerMovement) return;
        playerMovementWasEnabled = playerMovement.enabled;
        playerMovement.enabled = false;
        var body = playerMovement.GetComponent<Rigidbody2D>();
        if (body) body.linearVelocity = Vector2.zero;
    }

    void UnlockPlayer()
    {
        if (playerMovement) playerMovement.enabled = playerMovementWasEnabled;
        playerMovement = null;
    }

    void OnDisable()
    {
        StopAllCoroutines();
        panelOpen = false;
        animating = false;
        draggingGold = false;
        IsModalOpen = false;
        UnlockPlayer();
    }
}
