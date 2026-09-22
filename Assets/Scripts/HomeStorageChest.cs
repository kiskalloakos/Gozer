using System.Collections;
using System.Collections.Generic;
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
    readonly GoldStackCursor goldCursor = new GoldStackCursor();
    readonly HashSet<int> rightDragVisited = new HashSet<int>();
    bool rightDragging;
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
        goldCursor.ReturnHeld();
        rightDragging = false;
        rightDragVisited.Clear();
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
        PlayerInventoryUI.DrawQuickbarSlotSelection(playerPanel, FindAnyObjectByType<ExpeditionHUD>());

        bool showTooltip = false;
        for (int slot = 0; slot < GoldInventoryLocation.PlayerSlotCount; slot++)
        {
            int amount = GoldInventoryLocation.GetAmount(
                GoldInventoryLocation.Container.PlayerInventory, slot);
            if (amount > 0) PlayerInventoryUI.DrawGold(playerPanel, slot, amount);
            if (amount > 0 && Event.current != null
                && PlayerInventoryUI.GetSlotRect(playerPanel, slot).Contains(Event.current.mousePosition))
                showTooltip = true;
        }
        for (int slot = 0; slot < GoldInventoryLocation.ChestSlotCount; slot++)
        {
            int amount = GoldInventoryLocation.GetAmount(
                GoldInventoryLocation.Container.HomeChest, slot);
            if (amount > 0) PlayerInventoryUI.DrawGold(chestPanel, slot, amount);
            if (amount > 0 && Event.current != null
                && PlayerInventoryUI.GetSlotRect(chestPanel, slot).Contains(Event.current.mousePosition))
                showTooltip = true;
        }
        PlayerInventoryUI.DrawQuickbarSlotHotkeys(playerPanel);
        if (showTooltip) PlayerInventoryUI.DrawItemTooltip(Event.current.mousePosition, "GOLD");

        HandlePointer(playerPanel, chestPanel);
        if (goldCursor.IsHolding && Event.current != null)
        {
            var dragRect = new Rect(Event.current.mousePosition.x - 25f, Event.current.mousePosition.y - 25f, 50f, 50f);
            ExpeditionHUD.DrawGoldStack(dragRect, goldCursor.TotalAmount);
        }

        GUI.color = previousColor;
    }

    void HandlePointer(Rect playerPanel, Rect chestPanel)
    {
        Event currentEvent = Event.current;
        if (currentEvent == null) return;

        bool overSlot = TryGetTarget(currentEvent.mousePosition, playerPanel, chestPanel,
            out GoldInventoryLocation.Container container, out int slot);
        int slotAmount = overSlot ? GoldInventoryLocation.GetAmount(container, slot) : 0;
        CursorClickFeedback.SetInteractiveHover(overSlot && (slotAmount > 0 || goldCursor.IsHolding));

        if (currentEvent.type == EventType.MouseDown)
        {
            if (overSlot && currentEvent.button == 0)
            {
                goldCursor.LeftClick(container, slot);
                currentEvent.Use();
            }
            else if (overSlot && currentEvent.button == 1)
            {
                rightDragVisited.Clear();
                rightDragVisited.Add(TargetId(container, slot));
                rightDragging = true;
                goldCursor.RightClick(container, slot);
                currentEvent.Use();
            }
            else if ((playerPanel.Contains(currentEvent.mousePosition) || chestPanel.Contains(currentEvent.mousePosition))
                && currentEvent.button == 0)
            {
                currentEvent.Use();
            }
            else if (currentEvent.button == 0)
            {
                Close();
                currentEvent.Use();
            }
        }
        else if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 1 && rightDragging)
        {
            if (overSlot && rightDragVisited.Add(TargetId(container, slot)) && goldCursor.IsHolding)
                goldCursor.RightClick(container, slot);
            currentEvent.Use();
        }
        else if (currentEvent.type == EventType.MouseUp && currentEvent.button == 1 && rightDragging)
        {
            rightDragging = false;
            rightDragVisited.Clear();
            currentEvent.Use();
        }
    }

    static bool TryGetTarget(Vector2 pointer, Rect playerPanel, Rect chestPanel,
        out GoldInventoryLocation.Container container, out int slot)
    {
        if (PlayerInventoryUI.TryGetSlotAt(pointer, playerPanel, out slot))
        {
            container = GoldInventoryLocation.Container.PlayerInventory;
            return true;
        }
        if (PlayerInventoryUI.TryGetSlotAt(pointer, chestPanel, out slot))
        {
            container = GoldInventoryLocation.Container.HomeChest;
            return true;
        }
        container = GoldInventoryLocation.Container.PlayerInventory;
        slot = -1;
        return false;
    }

    static int TargetId(GoldInventoryLocation.Container container, int slot)
        => (int)container * GoldInventoryLocation.PlayerSlotCount + slot;

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
        goldCursor.ReturnHeld();
        rightDragging = false;
        rightDragVisited.Clear();
        IsModalOpen = false;
        UnlockPlayer();
    }
}
