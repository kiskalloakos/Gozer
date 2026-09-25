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
    readonly InventoryStackCursor itemCursor = new InventoryStackCursor();
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
        if (animating || PlayerInventoryUI.IsOpen || WorkbenchCraftingUI.IsModalOpen) return;
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
        itemCursor.ReturnHeld();
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
        var hud = FindAnyObjectByType<ExpeditionHUD>();
        PlayerInventoryUI.DrawQuickbarSlotSelection(playerPanel, hud);

        string tooltipName = null;
        for (int slot = 0; slot < ItemInventory.PlayerSlotCount; slot++)
        {
            ItemStack stack = ItemInventory.GetStack(ItemInventory.Container.PlayerInventory, slot);
            if (stack.item != InventoryItemId.Empty && stack.amount > 0)
                PlayerInventoryUI.DrawItemStack(playerPanel, slot, stack.item, stack.amount,
                    hud ? hud.GetStackCountBounce(slot) : 0f);
            if (stack.item != InventoryItemId.Empty && Event.current != null
                && PlayerInventoryUI.GetSlotRect(playerPanel, slot).Contains(Event.current.mousePosition))
                tooltipName = ItemName(stack.item);
        }
        for (int slot = 0; slot < ItemInventory.ChestSlotCount; slot++)
        {
            ItemStack stack = ItemInventory.GetStack(ItemInventory.Container.HomeChest, slot);
            if (stack.item != InventoryItemId.Empty && stack.amount > 0)
                PlayerInventoryUI.DrawItemStack(chestPanel, slot, stack.item, stack.amount);
            if (stack.item != InventoryItemId.Empty && Event.current != null
                && PlayerInventoryUI.GetSlotRect(chestPanel, slot).Contains(Event.current.mousePosition))
                tooltipName = ItemName(stack.item);
        }
        PlayerInventoryUI.DrawQuickbarSlotHotkeys(playerPanel);
        if (tooltipName != null) PlayerInventoryUI.DrawItemTooltip(Event.current.mousePosition, tooltipName);

        HandlePointer(playerPanel, chestPanel);
        if (itemCursor.IsHolding && Event.current != null)
        {
            var dragRect = new Rect(Event.current.mousePosition.x - 25f, Event.current.mousePosition.y - 25f, 50f, 50f);
            ExpeditionHUD.DrawItemStack(dragRect, itemCursor.HeldItem, itemCursor.Amount);
        }

        GUI.color = previousColor;
    }

    void HandlePointer(Rect playerPanel, Rect chestPanel)
    {
        Event currentEvent = Event.current;
        if (currentEvent == null) return;

        bool overSlot = TryGetTarget(currentEvent.mousePosition, playerPanel, chestPanel,
            out ItemInventory.Container container, out int slot);
        ItemStack stack = overSlot ? ItemInventory.GetStack(container, slot) : default;
        bool occupied = overSlot && stack.item != InventoryItemId.Empty;
        CursorClickFeedback.SetInteractiveHover(overSlot && (occupied || itemCursor.IsHolding));

        if (currentEvent.type == EventType.MouseDown)
        {
            if (overSlot && currentEvent.button == 0)
            {
                itemCursor.LeftClick(container, slot);
                currentEvent.Use();
            }
            else if (overSlot && currentEvent.button == 1)
            {
                rightDragVisited.Clear();
                rightDragVisited.Add(TargetId(container, slot));
                rightDragging = true;
                itemCursor.RightClick(container, slot);
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
            if (overSlot && rightDragVisited.Add(TargetId(container, slot)) && itemCursor.IsHolding)
                itemCursor.RightClick(container, slot);
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
        out ItemInventory.Container container, out int slot)
    {
        if (PlayerInventoryUI.TryGetSlotAt(pointer, playerPanel, out slot))
        {
            container = ItemInventory.Container.PlayerInventory;
            return true;
        }
        if (PlayerInventoryUI.TryGetSlotAt(pointer, chestPanel, out slot))
        {
            container = ItemInventory.Container.HomeChest;
            return true;
        }
        container = ItemInventory.Container.PlayerInventory;
        slot = -1;
        return false;
    }

    static int TargetId(ItemInventory.Container container, int slot)
        => (int)container * ItemInventory.PlayerSlotCount + slot;

    static string ItemName(InventoryItemId item)
        => ItemInventory.GetDisplayName(item);

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
        itemCursor.ReturnHeld();
        rightDragging = false;
        rightDragVisited.Clear();
        IsModalOpen = false;
        UnlockPlayer();
    }
}
