using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Paired inventory/workbench modal. Recipes are intentionally small for now:
/// five Wood starts a five-second craft for one wooden tool.
/// </summary>
public sealed class WorkbenchCraftingUI : MonoBehaviour
{
    const int WoodPerTool = 5;
    const float BackdropOpacity = .72f;
    const int RecipeCount = 4;
    static readonly InventoryItemId[] Recipes =
    {
        InventoryItemId.Sword,
        InventoryItemId.Axe,
        InventoryItemId.Pickaxe,
        InventoryItemId.Shovel
    };

    public float craftDuration = 5f;

    public static bool IsModalOpen { get; private set; }

    readonly InventoryStackCursor itemCursor = new InventoryStackCursor();
    readonly HashSet<int> rightDragVisited = new HashSet<int>();
    bool rightDragging;
    bool panelOpen;
    bool crafting;
    float craftStartedAt;
    InventoryItemId craftingItem;
    TownPlayerController playerMovement;
    bool playerMovementWasEnabled;

    public static void OpenFor(TownInteractable workbench)
    {
        if (!workbench || IsModalOpen) return;
        var ui = workbench.GetComponent<WorkbenchCraftingUI>();
        if (!ui) ui = workbench.gameObject.AddComponent<WorkbenchCraftingUI>();
        ui.Open();
    }

    void Open()
    {
        if (panelOpen) return;
        ItemInventory.EnsureLoaded();
        panelOpen = true;
        IsModalOpen = true;
        LockPlayer();
    }

    void Update()
    {
        if (!panelOpen) return;

        if (crafting)
        {
            if (Time.unscaledTime - craftStartedAt >= Mathf.Max(.01f, craftDuration))
                CompleteCraft();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
            Close();
    }

    void Close()
    {
        if (!panelOpen || crafting) return;
        itemCursor.ReturnHeld(GetHUD());
        rightDragging = false;
        rightDragVisited.Clear();
        panelOpen = false;
        IsModalOpen = false;
        UnlockPlayer();
    }

    void OnGUI()
    {
        if (!panelOpen) return;

        GUI.depth = -1000;
        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, BackdropOpacity);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

        float scale = PlayerInventoryUI.GetPanelScale();
        float panelWidth = 110f * scale;
        float gap = 16f * scale;
        Vector2 center = new Vector2(Screen.width * .5f, Screen.height * .5f);
        Rect playerPanel = PlayerInventoryUI.GetPanelRectAt(
            new Vector2(center.x - (panelWidth + gap) * .5f, center.y));
        Rect workbenchPanel = PlayerInventoryUI.GetPanelRectAt(
            new Vector2(center.x + (panelWidth + gap) * .5f, center.y));

        DrawPlayerPanel(playerPanel);
        DrawWorkbenchPanel(workbenchPanel, scale);
        HandlePointer(playerPanel, workbenchPanel);

        if (itemCursor.IsHolding && Event.current != null)
        {
            Rect dragRect = new Rect(Event.current.mousePosition.x - 25f,
                Event.current.mousePosition.y - 25f, 50f, 50f);
            if (ItemInventory.IsTool(itemCursor.HeldItem))
                ExpeditionHUD.DrawItemIcon(dragRect, itemCursor.HeldItem);
            else if (itemCursor.HeldItem == InventoryItemId.Wood)
                ExpeditionHUD.DrawWoodStack(dragRect, itemCursor.Amount);
            else if (itemCursor.HeldItem == InventoryItemId.Gold)
                ExpeditionHUD.DrawGoldStack(dragRect, itemCursor.Amount);
        }

        GUI.color = previousColor;
    }

    void DrawPlayerPanel(Rect panel)
    {
        PlayerInventoryUI.DrawPanel(panel, "INVENTORY");
        PlayerInventoryUI.DrawQuickbarSlotSelection(panel, GetHUD());

        string tooltipName = null;
        for (int slot = 0; slot < ItemInventory.PlayerSlotCount; slot++)
        {
            ItemStack stack = ItemInventory.GetStack(ItemInventory.Container.PlayerInventory, slot);
            if (ItemInventory.IsTool(stack.item)) PlayerInventoryUI.DrawTool(panel, slot, stack.item);
            else if (stack.item == InventoryItemId.Wood) PlayerInventoryUI.DrawWood(panel, slot, stack.amount);
            else if (stack.item == InventoryItemId.Gold) PlayerInventoryUI.DrawGold(panel, slot, stack.amount);

            if (stack.item != InventoryItemId.Empty && Event.current != null
                && PlayerInventoryUI.GetSlotRect(panel, slot).Contains(Event.current.mousePosition))
                tooltipName = ItemName(stack.item);
        }

        PlayerInventoryUI.DrawQuickbarSlotHotkeys(panel);
        if (tooltipName != null)
            PlayerInventoryUI.DrawItemTooltip(Event.current.mousePosition, tooltipName);
    }

    void DrawWorkbenchPanel(Rect panel, float scale)
    {
        PlayerInventoryUI.DrawPanel(panel, "WORKBENCH");
        int wood = ItemInventory.GetTotal(ItemInventory.Container.PlayerInventory, InventoryItemId.Wood);
        string hoveredRecipeName = null;

        if (!crafting && wood >= WoodPerTool)
        {
            for (int recipeIndex = 0; recipeIndex < RecipeCount; recipeIndex++)
            {
                InventoryItemId item = Recipes[recipeIndex];
                Rect slot = PlayerInventoryUI.GetSlotRect(panel, recipeIndex);
                float inset = Mathf.Max(2f, panel.width / 110f);
                ExpeditionHUD.DrawItemIcon(new Rect(slot.x + inset, slot.y + inset,
                    slot.width - inset * 2f, slot.height - inset * 2f), item);
                ExpeditionHUD.DrawPixelTextAt("1X", slot.center.x,
                    slot.yMax - 7f * scale, Mathf.Max(1f, scale));
                if (Event.current != null && slot.Contains(Event.current.mousePosition))
                    hoveredRecipeName = ItemName(item);
            }
        }
        else if (!crafting)
        {
            ExpeditionHUD.DrawPixelTextAt("NEED 5 WOOD", panel.center.x,
                panel.y + 42f * scale, Mathf.Max(1f, scale));
        }

        if (crafting)
            DrawCraftProgress(panel, scale);
        else if (hoveredRecipeName != null)
            PlayerInventoryUI.DrawItemTooltip(Event.current.mousePosition, hoveredRecipeName);
    }

    void DrawCraftProgress(Rect panel, float scale)
    {
        float progress = Mathf.Clamp01((Time.unscaledTime - craftStartedAt) /
            Mathf.Max(.01f, craftDuration));
        Color oldColor = GUI.color;
        GUI.color = new Color(.08f, .04f, .025f, .86f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = new Color(1f, .76f, .28f, .95f);
        Rect bar = new Rect(panel.x + 16f * scale, panel.yMax - 25f * scale,
            (panel.width - 32f * scale) * progress, 6f * scale);
        GUI.DrawTexture(bar, Texture2D.whiteTexture);
        GUI.color = Color.white;
        ExpeditionHUD.DrawPixelTextAt("CRAFTING", panel.center.x,
            panel.y + 35f * scale, Mathf.Max(1f, scale));
        ExpeditionHUD.DrawPixelTextAt(ItemName(craftingItem), panel.center.x,
            panel.y + 52f * scale, Mathf.Max(1f, scale));
        GUI.color = oldColor;
    }

    void HandlePointer(Rect playerPanel, Rect workbenchPanel)
    {
        Event currentEvent = Event.current;
        if (currentEvent == null) return;

        if (crafting)
        {
            CursorClickFeedback.SetInteractiveHover(false);
            if (currentEvent.type == EventType.MouseDown)
                currentEvent.Use();
            return;
        }

        bool overPlayerSlot = PlayerInventoryUI.TryGetSlotAt(currentEvent.mousePosition,
            playerPanel, out int playerSlot);
        bool overRecipe = TryGetRecipeAt(currentEvent.mousePosition, workbenchPanel,
            out InventoryItemId recipe);
        ItemStack stack = overPlayerSlot
            ? ItemInventory.GetStack(ItemInventory.Container.PlayerInventory, playerSlot)
            : default;
        bool occupied = overPlayerSlot && stack.item != InventoryItemId.Empty;
        CursorClickFeedback.SetInteractiveHover((overPlayerSlot && (occupied || itemCursor.IsHolding))
            || (overRecipe && ItemInventory.GetTotal(ItemInventory.Container.PlayerInventory,
                InventoryItemId.Wood) >= WoodPerTool && !itemCursor.IsHolding));

        if (currentEvent.type == EventType.MouseDown)
        {
            if (overRecipe && !itemCursor.IsHolding && currentEvent.button == 0)
            {
                StartCraft(recipe);
                currentEvent.Use();
            }
            else if (overPlayerSlot && currentEvent.button == 0)
            {
                itemCursor.LeftClick(ItemInventory.Container.PlayerInventory, playerSlot, GetHUD());
                currentEvent.Use();
            }
            else if (overPlayerSlot && currentEvent.button == 1)
            {
                rightDragVisited.Clear();
                rightDragVisited.Add(playerSlot);
                rightDragging = true;
                itemCursor.RightClick(ItemInventory.Container.PlayerInventory, playerSlot, GetHUD());
                currentEvent.Use();
            }
            else if ((playerPanel.Contains(currentEvent.mousePosition)
                || workbenchPanel.Contains(currentEvent.mousePosition)) && currentEvent.button == 0)
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
            if (overPlayerSlot && rightDragVisited.Add(playerSlot) && itemCursor.IsHolding)
                itemCursor.RightClick(ItemInventory.Container.PlayerInventory, playerSlot, GetHUD());
            currentEvent.Use();
        }
        else if (currentEvent.type == EventType.MouseUp && currentEvent.button == 1 && rightDragging)
        {
            rightDragging = false;
            rightDragVisited.Clear();
            currentEvent.Use();
        }
    }

    void StartCraft(InventoryItemId item)
    {
        if (crafting || ItemInventory.GetTotal(ItemInventory.Container.PlayerInventory,
            InventoryItemId.Wood) < WoodPerTool) return;

        bool hasResultStack = ItemInventory.FindItemSlot(ItemInventory.Container.PlayerInventory, item) >= 0;
        if (!hasResultStack && ItemInventory.FindEmptySlot(ItemInventory.Container.PlayerInventory) < 0)
        {
            ShowNotice("Your inventory is full.");
            return;
        }

        ItemInventory.RemoveAmount(ItemInventory.Container.PlayerInventory, InventoryItemId.Wood, WoodPerTool);
        craftingItem = item;
        craftStartedAt = Time.unscaledTime;
        crafting = true;
    }

    void CompleteCraft()
    {
        crafting = false;
        if (ItemInventory.AddItem(ItemInventory.Container.PlayerInventory, craftingItem, 1))
            ShowNotice($"Crafted 1x {ItemName(craftingItem)}.", 2.5f);
        else
        {
            ItemInventory.AddItem(ItemInventory.Container.PlayerInventory, InventoryItemId.Wood, WoodPerTool);
            ShowNotice("Your inventory filled up before crafting finished. Wood returned.", 4f);
        }
    }

    static bool TryGetRecipeAt(Vector2 pointer, Rect panel, out InventoryItemId item)
    {
        for (int index = 0; index < RecipeCount; index++)
        {
            if (!PlayerInventoryUI.GetSlotRect(panel, index).Contains(pointer)) continue;
            item = Recipes[index];
            return true;
        }
        item = InventoryItemId.Empty;
        return false;
    }

    ExpeditionHUD GetHUD()
    {
        var hud = FindAnyObjectByType<ExpeditionHUD>();
        return hud;
    }

    void ShowNotice(string message, float seconds = 3.5f)
    {
        if (HomeInteriorController.Instance)
            HomeInteriorController.Instance.ShowNotice(message, seconds);
        else if (TownHubController.Instance)
            TownHubController.Instance.ShowNotice(message, seconds);
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

    static string ItemName(InventoryItemId item)
        => item == InventoryItemId.Axe ? "AXE"
            : item == InventoryItemId.Pickaxe ? "PICKAXE"
            : item == InventoryItemId.Sword ? "SWORD"
            : item == InventoryItemId.Shovel ? "SHOVEL" : "TOOL";

    void OnDisable()
    {
        StopAllCoroutines();
        itemCursor.ReturnHeld(GetHUD());
        rightDragging = false;
        rightDragVisited.Clear();
        panelOpen = false;
        crafting = false;
        IsModalOpen = false;
        UnlockPlayer();
    }
}
