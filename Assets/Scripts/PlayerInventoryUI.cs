using System.Collections.Generic;
using UnityEngine;

/// <summary>Modal player bag shown with E. The chest reuses the same 4 x 4 art and slot geometry.</summary>
public class PlayerInventoryUI : MonoBehaviour
{
    const int Columns = 4;
    const float BackdropOpacity = .72f;
    static Texture2D inventoryArt;

    public static bool IsOpen { get; private set; }

    readonly InventoryStackCursor itemCursor = new InventoryStackCursor();
    readonly HashSet<int> rightDragVisited = new HashSet<int>();
    bool rightDragging;
    TownPlayerController playerMovement;
    bool playerMovementWasEnabled;

    void Update()
    {
        if (GameSessionFlow.IsBlockingGameplay) return;
        if (HomeStorageChest.IsModalOpen || WorkbenchCraftingUI.IsModalOpen) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (IsOpen) Close();
            else Open();
        }
        else if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    void Open()
    {
        IsOpen = true;
        LockPlayer();
    }

    void Close()
    {
        itemCursor.ReturnHeld(GetHUD());
        rightDragging = false;
        rightDragVisited.Clear();
        IsOpen = false;
        UnlockPlayer();
    }

    void OnGUI()
    {
        if (!IsOpen) return;

        GUI.depth = -1000;
        var oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, BackdropOpacity);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

        Rect panel = GetCenteredPanelRect();
        DrawPanel(panel, "INVENTORY");

        var hud = GetHUD();
        DrawQuickbarSlotSelection(panel, hud);
        int hoveredSlot = -1;
        for (int slot = 0; slot < ItemInventory.PlayerSlotCount; slot++)
        {
            ItemStack stack = ItemInventory.GetStack(ItemInventory.Container.PlayerInventory, slot);
            int amount = itemCursor.GetGoldSlotAmount(ItemInventory.Container.PlayerInventory, slot, hud);
            if (ItemInventory.IsTool(stack.item)) DrawTool(panel, slot, stack.item);
            else if (stack.item == InventoryItemId.Wood) DrawWood(panel, slot, stack.amount);
            else if (amount > 0) DrawGold(panel, slot, amount);
            if ((stack.item != InventoryItemId.Empty || amount > 0) && Event.current != null
                && GetSlotRect(panel, slot).Contains(Event.current.mousePosition))
                hoveredSlot = slot;
        }
        DrawQuickbarSlotHotkeys(panel);

        if (hoveredSlot >= 0)
        {
            ItemStack stack = ItemInventory.GetStack(ItemInventory.Container.PlayerInventory, hoveredSlot);
            DrawItemTooltip(Event.current.mousePosition,
                stack.item == InventoryItemId.Empty ? "GOLD" : ItemName(stack.item));
        }

        HandlePointer(panel, hud);

        if (itemCursor.IsHolding && Event.current != null)
        {
            var dragRect = new Rect(Event.current.mousePosition.x - 25f, Event.current.mousePosition.y - 25f, 50f, 50f);
            if (ItemInventory.IsTool(itemCursor.HeldItem)) ExpeditionHUD.DrawItemIcon(dragRect, itemCursor.HeldItem);
            else if (itemCursor.HeldItem == InventoryItemId.Wood)
                ExpeditionHUD.DrawWoodStack(dragRect, itemCursor.Amount);
            else if (itemCursor.HeldItem == InventoryItemId.Gold)
                ExpeditionHUD.DrawGoldStack(dragRect, itemCursor.Amount);
        }

        GUI.color = oldColor;
    }

    void HandlePointer(Rect panel, ExpeditionHUD hud)
    {
        Event currentEvent = Event.current;
        if (currentEvent == null) return;
        bool overSlot = TryGetSlotAt(currentEvent.mousePosition, panel, out int slot);
        ItemStack stack = overSlot
            ? ItemInventory.GetStack(ItemInventory.Container.PlayerInventory, slot) : default;
        int slotAmount = overSlot
            ? itemCursor.GetGoldSlotAmount(ItemInventory.Container.PlayerInventory, slot, hud) : 0;
        bool occupied = overSlot && (stack.item != InventoryItemId.Empty || slotAmount > 0);
        CursorClickFeedback.SetInteractiveHover(overSlot && (occupied || itemCursor.IsHolding));

        if (currentEvent.type == EventType.MouseDown)
        {
            if (overSlot && currentEvent.button == 0)
            {
                itemCursor.LeftClick(ItemInventory.Container.PlayerInventory, slot, hud);
                currentEvent.Use();
            }
            else if (overSlot && currentEvent.button == 1)
            {
                rightDragVisited.Clear();
                rightDragVisited.Add(slot);
                rightDragging = true;
                itemCursor.RightClick(ItemInventory.Container.PlayerInventory, slot, hud);
                currentEvent.Use();
            }
            else if (panel.Contains(currentEvent.mousePosition) && currentEvent.button == 0)
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
            if (overSlot && rightDragVisited.Add(slot) && itemCursor.IsHolding)
                itemCursor.RightClick(ItemInventory.Container.PlayerInventory, slot, hud);
            currentEvent.Use();
        }
        else if (currentEvent.type == EventType.MouseUp && currentEvent.button == 1 && rightDragging)
        {
            rightDragging = false;
            rightDragVisited.Clear();
            currentEvent.Use();
        }
    }

    ExpeditionHUD GetHUD()
    {
        var hud = GetComponent<ExpeditionHUD>();
        return hud ? hud : FindAnyObjectByType<ExpeditionHUD>();
    }

    public static Rect GetCenteredPanelRect()
    {
        Texture2D art = GetInventoryArt();
        float scale = GetPanelScale();
        float width = (art ? art.width : 110f) * scale;
        float height = (art ? art.height : 100f) * scale;
        return new Rect(Mathf.Round((Screen.width - width) * .5f), Mathf.Round((Screen.height - height) * .5f), width, height);
    }

    public static Rect GetPanelRectAt(Vector2 center)
    {
        Texture2D art = GetInventoryArt();
        float scale = GetPanelScale();
        float width = (art ? art.width : 110f) * scale;
        float height = (art ? art.height : 100f) * scale;
        return new Rect(Mathf.Round(center.x - width * .5f), Mathf.Round(center.y - height * .5f), width, height);
    }

    public static void DrawPanel(Rect panel, string title)
    {
        GUI.color = Color.white;
        GUI.DrawTexture(panel, GetInventoryArt() ? GetInventoryArt() : Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
        float scale = panel.width / 110f;
        var label = ExpeditionHUD.CreateLabelStyle(Mathf.RoundToInt(10f * scale));
        label.normal.textColor = new Color(1f, .83f, .39f);
        GUI.Label(new Rect(panel.x, panel.y - 18f * scale, panel.width, 16f * scale), title, label);
    }

    public static Rect GetSlotRect(Rect panel, int slot)
    {
        float scale = panel.width / 110f;
        // These are the actual cell interiors in inventory.png, not the outer frame bounds.
        const float sourceGridX = 20f;
        const float sourceGridY = 16f;
        const float sourceCellSize = 17f;
        float cellSize = sourceCellSize * scale;
        return new Rect(panel.x + sourceGridX * scale + (slot % Columns) * cellSize,
            panel.y + sourceGridY * scale + (slot / Columns) * cellSize, cellSize, cellSize);
    }

    public static bool TryGetSlotAt(Vector2 pointer, Rect panel, out int slot)
    {
        for (int i = 0; i < ItemInventory.PlayerSlotCount; i++)
        {
            if (!GetSlotRect(panel, i).Contains(pointer)) continue;
            slot = i;
            return true;
        }
        slot = -1;
        return false;
    }

    public static void DrawGold(Rect panel, int slot, int amount)
    {
        Rect slotRect = GetSlotRect(panel, slot);
        float inset = Mathf.Max(2f, panel.width / 110f);
        ExpeditionHUD.DrawGoldStack(new Rect(slotRect.x + inset, slotRect.y + inset,
            slotRect.width - inset * 2f, slotRect.height - inset * 2f), amount,
            (slotRect.width - inset * 2f) / 50f);
    }

    public static void DrawWood(Rect panel, int slot, int amount)
    {
        Rect slotRect = GetSlotRect(panel, slot);
        float inset = Mathf.Max(2f, panel.width / 110f);
        ExpeditionHUD.DrawWoodStack(new Rect(slotRect.x + inset, slotRect.y + inset,
            slotRect.width - inset * 2f, slotRect.height - inset * 2f), amount,
            (slotRect.width - inset * 2f) / 50f);
    }

    public static void DrawTool(Rect panel, int slot)
        => DrawTool(panel, slot, InventoryItemId.Axe);

    public static void DrawTool(Rect panel, int slot, InventoryItemId item)
    {
        Rect slotRect = GetSlotRect(panel, slot);
        float inset = Mathf.Max(2f, panel.width / 110f);
        ExpeditionHUD.DrawItemIcon(new Rect(slotRect.x + inset, slotRect.y + inset,
            slotRect.width - inset * 2f, slotRect.height - inset * 2f), item);
    }

    public static void DrawQuickbarSlotSelection(Rect panel, ExpeditionHUD hud)
    {
        if (!hud) return;
        int activeSlot = hud.ActiveQuickbarSlot;
        if (activeSlot < 0 || activeSlot >= ExpeditionHUD.QuickbarSlotCount) return;
        Rect slot = GetSlotRect(panel, activeSlot);
        float thickness = Mathf.Max(1f, panel.width / 110f);
        GUI.color = new Color(1f, .8f, .22f, .2f);
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
        for (int slot = 0; slot < ExpeditionHUD.QuickbarSlotCount; slot++)
        {
            Rect slotRect = GetSlotRect(panel, slot);
            ExpeditionHUD.DrawPixelTextAt((slot + 1).ToString(), slotRect.x + pixel * 3f,
                slotRect.y + pixel * 2f, pixel);
        }
    }

    public static void DrawItemTooltip(Vector2 pointer, string itemName)
    {
        float scale = GetPanelScale();
        float width = 34f * scale;
        float height = 11f * scale;
        var rect = new Rect(Mathf.Clamp(pointer.x - width * .5f, 4f, Screen.width - width - 4f),
            Mathf.Max(4f, pointer.y - height - 8f * scale), width, height);
        var oldColor = GUI.color;
        GUI.color = new Color(.08f, .045f, .03f, .96f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
        var label = ExpeditionHUD.CreateLabelStyle(Mathf.Max(7, Mathf.RoundToInt(6f * scale)));
        label.normal.textColor = new Color(1f, .82f, .35f);
        GUI.Label(rect, itemName, label);
        GUI.color = oldColor;
    }

    public static float GetPanelScale() => ExpeditionHUD.GetPixelScale();

    static Texture2D GetInventoryArt()
    {
        if (!inventoryArt) inventoryArt = Resources.Load<Texture2D>("UI/inventory");
        return inventoryArt;
    }

    static string ItemName(InventoryItemId item)
        => item == InventoryItemId.Axe ? "AXE"
            : item == InventoryItemId.Pickaxe ? "PICKAXE"
            : item == InventoryItemId.Sword ? "SWORD"
            : item == InventoryItemId.Shovel ? "SHOVEL"
            : item == InventoryItemId.Wood ? "WOOD"
            : item == InventoryItemId.Gold ? "GOLD" : "ITEM";

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
        itemCursor.ReturnHeld(GetHUD());
        if (IsOpen) Close();
    }
}
