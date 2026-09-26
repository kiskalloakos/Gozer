using System;
using System.Collections.Generic;
using UnityEngine;

public enum InventoryItemId
{
    Empty = 0,
    Gold = 1,
    Wood = 2,
    Axe = 3,
    Pickaxe = 4,
    Sword = 5,
    Shovel = 6
}

[Serializable]
public struct ItemStack
{
    public InventoryItemId item;
    public int amount;
    // Gold picked up during an expedition remains at risk until extraction.
    // This is a portion of amount, not a separate inventory store.
    public int unsecuredAmount;

    public ItemStack(InventoryItemId item, int amount, int unsecuredAmount = 0)
    {
        this.item = amount > 0 ? item : InventoryItemId.Empty;
        this.amount = amount > 0 ? amount : 0;
        this.unsecuredAmount = this.item == InventoryItemId.Gold
            ? Mathf.Clamp(unsecuredAmount, 0, this.amount) : 0;
    }
}

/// <summary>
/// The single runtime inventory store used by both the player bag and the home chest.
/// Each slot contains an item id and quantity, so adding future items does not
/// require another inventory class or another cursor implementation.
/// </summary>
public static class ItemInventory
{
    /// <summary>Raised after a world pickup enters a player inventory slot.</summary>
    public static event Action<int> PlayerItemPickedUp;

    static readonly InventoryItemId[] ToolIds =
    {
        InventoryItemId.Axe,
        InventoryItemId.Pickaxe,
        InventoryItemId.Sword,
        InventoryItemId.Shovel
    };

    public static IReadOnlyList<InventoryItemId> ToolItemIds => ToolIds;

    public enum Container
    {
        PlayerInventory = 0,
        HomeChest = 1
    }

    public const int PlayerSlotCount = 16;
    public const int ChestSlotCount = 16;
    const int CurrentVersion = 2;
    const string VersionKey = "Inventory.ItemsVersion";
    const string SlotKeyPrefix = "Inventory.Item";

    static readonly List<ItemStack> playerSlots = new List<ItemStack>();
    static readonly List<ItemStack> chestSlots = new List<ItemStack>();
    static bool loaded;

    public static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        InitializeSlots(playerSlots, PlayerSlotCount);
        InitializeSlots(chestSlots, ChestSlotCount);

        int savedVersion = PlayerPrefs.GetInt(VersionKey, 0);
        if (savedVersion >= 1)
        {
            LoadSlots(Container.PlayerInventory, playerSlots, savedVersion >= 2);
            LoadSlots(Container.HomeChest, chestSlots, savedVersion >= 2);
            if (savedVersion < CurrentVersion) SaveAll();
            return;
        }

        MigrateLegacyStores();
        SaveAll();
    }

    public static ItemStack GetStack(Container container, int slot)
    {
        EnsureLoaded();
        return Slots(container)[ClampSlot(container, slot)];
    }

    public static ItemStack[] ReadSlots(Container container)
    {
        EnsureLoaded();
        return Slots(container).ToArray();
    }

    public static void WriteSlots(Container container, ItemStack[] values)
    {
        EnsureLoaded();
        List<ItemStack> slots = Slots(container);
        for (int slot = 0; slot < slots.Count; slot++)
            slots[slot] = values != null && slot < values.Length
                ? Normalize(values[slot], container) : new ItemStack();
        SaveAll();
    }

    public static void WriteBothSlots(ItemStack[] player, ItemStack[] chest)
    {
        EnsureLoaded();
        CopySlots(playerSlots, player, Container.PlayerInventory);
        CopySlots(chestSlots, chest, Container.HomeChest);
        SaveAll();
    }

    static void CopySlots(List<ItemStack> destination, ItemStack[] values, Container container)
    {
        for (int slot = 0; slot < destination.Count; slot++)
            destination[slot] = values != null && slot < values.Length
                ? Normalize(values[slot], container) : new ItemStack();
    }

    public static InventoryItemId GetItem(Container container, int slot)
        => GetStack(container, slot).item;

    public static int GetAmount(Container container, int slot, InventoryItemId item)
    {
        ItemStack stack = GetStack(container, slot);
        return stack.item == item ? stack.amount : 0;
    }

    public static void SetStack(Container container, int slot, InventoryItemId item, int amount,
        int unsecuredAmount = 0)
    {
        EnsureLoaded();
        int safeSlot = ClampSlot(container, slot);
        if (container != Container.PlayerInventory) unsecuredAmount = 0;
        if (amount <= 0 || item == InventoryItemId.Empty)
            Slots(container)[safeSlot] = new ItemStack();
        else
            Slots(container)[safeSlot] = new ItemStack(item, IsTool(item) ? 1 : amount, unsecuredAmount);
        SaveAll();
    }

    public static bool AddToSlot(Container container, int slot, InventoryItemId item, int amount,
        int unsecuredAmount = 0)
    {
        if (amount <= 0 || item == InventoryItemId.Empty) return false;
        EnsureLoaded();
        int safeSlot = ClampSlot(container, slot);
        ItemStack current = Slots(container)[safeSlot];
        if (IsTool(item) && (amount != 1 || current.item != InventoryItemId.Empty)) return false;
        if (current.item != InventoryItemId.Empty && current.item != item) return false;
        if (container != Container.PlayerInventory || item != InventoryItemId.Gold) unsecuredAmount = 0;
        Slots(container)[safeSlot] = new ItemStack(item, current.amount + amount,
            current.unsecuredAmount + Mathf.Clamp(unsecuredAmount, 0, amount));
        SaveAll();
        return true;
    }

    public static bool AddItem(Container container, InventoryItemId item, int amount,
        int preferredSlot = -1, int unsecuredAmount = 0)
        => AddItem(container, item, amount, preferredSlot, unsecuredAmount, false);

    /// <summary>
    /// Adds a collected world item and notifies the HUD so its inventory count can animate.
    /// World pickup scripts should use this method after confirming collection succeeds.
    /// </summary>
    public static bool AddPickedUpItem(Container container, InventoryItemId item, int amount,
        int preferredSlot = -1, int unsecuredAmount = 0)
        => AddItem(container, item, amount, preferredSlot, unsecuredAmount, true);

    static bool AddItem(Container container, InventoryItemId item, int amount,
        int preferredSlot, int unsecuredAmount, bool wasPickedUp)
    {
        if (amount <= 0 || item == InventoryItemId.Empty) return false;
        EnsureLoaded();

        if (IsTool(item))
        {
            int emptySlots = 0;
            for (int slotIndex = 0; slotIndex < SlotCount(container); slotIndex++)
                if (IsEmpty(container, slotIndex)
                    && !InventoryStackCursor.IsReserved(container, slotIndex)) emptySlots++;
            if (emptySlots < amount) return false;

            int firstAddedSlot = -1;
            for (int i = 0; i < amount; i++)
            {
                int emptySlot = FindEmptySlot(container);
                if (firstAddedSlot < 0) firstAddedSlot = emptySlot;
                Slots(container)[emptySlot] = new ItemStack(item, 1);
            }
            SaveAll();
            if (wasPickedUp && container == Container.PlayerInventory)
                PlayerItemPickedUp?.Invoke(firstAddedSlot);
            return true;
        }

        int slot = FindItemSlot(container, item);
        if (slot < 0 && IsEmpty(container, preferredSlot)
            && !InventoryStackCursor.IsReserved(container, preferredSlot)) slot = preferredSlot;
        if (slot < 0) slot = FindEmptySlot(container);
        if (slot < 0) return false;

        ItemStack current = Slots(container)[slot];
        if (container != Container.PlayerInventory || item != InventoryItemId.Gold) unsecuredAmount = 0;
        Slots(container)[slot] = new ItemStack(item, current.amount + amount,
            current.unsecuredAmount + Mathf.Clamp(item == InventoryItemId.Gold ? unsecuredAmount : 0, 0, amount));
        SaveAll();
        if (wasPickedUp && container == Container.PlayerInventory)
            PlayerItemPickedUp?.Invoke(slot);
        return true;
    }

    public static int FindItemSlot(Container container, InventoryItemId item)
    {
        EnsureLoaded();
        List<ItemStack> slots = Slots(container);
        for (int slot = 0; slot < slots.Count; slot++)
            if (slots[slot].item == item && slots[slot].amount > 0) return slot;
        return -1;
    }

    public static int FindEmptySlot(Container container)
    {
        EnsureLoaded();
        List<ItemStack> slots = Slots(container);
        for (int slot = 0; slot < slots.Count; slot++)
            if ((slots[slot].item == InventoryItemId.Empty || slots[slot].amount <= 0)
                && !InventoryStackCursor.IsReserved(container, slot)) return slot;
        return -1;
    }

    public static bool IsEmpty(Container container, int slot)
        => slot >= 0 && slot < SlotCount(container) && GetStack(container, slot).item == InventoryItemId.Empty;

    public static bool CanPlace(Container container, int slot, InventoryItemId item)
    {
        if (slot < 0 || slot >= SlotCount(container)) return false;
        ItemStack current = GetStack(container, slot);
        return current.item == InventoryItemId.Empty || (!IsTool(item) && current.item == item);
    }

    public static bool RemoveAmount(Container container, InventoryItemId item, int amount)
    {
        if (amount <= 0) return true;
        EnsureLoaded();
        int remaining = amount;
        List<ItemStack> slots = Slots(container);
        for (int slot = 0; slot < slots.Count && remaining > 0; slot++)
        {
            if (slots[slot].item != item) continue;
            int available = item == InventoryItemId.Gold
                ? slots[slot].amount - slots[slot].unsecuredAmount : slots[slot].amount;
            int removed = Mathf.Min(available, remaining);
            slots[slot] = new ItemStack(item, slots[slot].amount - removed,
                slots[slot].unsecuredAmount);
            remaining -= removed;
        }
        SaveAll();
        return remaining == 0;
    }

    public static int GetTotal(Container container, InventoryItemId item)
    {
        EnsureLoaded();
        int total = 0;
        foreach (ItemStack stack in Slots(container))
            if (stack.item == item) total += Mathf.Max(0, stack.amount);
        return total;
    }

    public static int GetTotal(InventoryItemId item)
        => GetTotal(Container.PlayerInventory, item) + GetTotal(Container.HomeChest, item);

    public static int GetSecuredGoldTotal(Container container)
    {
        EnsureLoaded();
        int total = 0;
        foreach (ItemStack stack in Slots(container))
            if (stack.item == InventoryItemId.Gold)
                total += Mathf.Max(0, stack.amount - stack.unsecuredAmount);
        return total;
    }

    public static int GetSecuredGoldTotal()
        => GetSecuredGoldTotal(Container.PlayerInventory) + GetSecuredGoldTotal(Container.HomeChest);

    public static bool IsTool(InventoryItemId item)
        => Array.IndexOf(ToolIds, item) >= 0;

    public static string GetDisplayName(InventoryItemId item)
        => item == InventoryItemId.Axe ? "WOODEN AXE"
            : item == InventoryItemId.Pickaxe ? "PICKAXE"
            : item == InventoryItemId.Sword ? "SWORD"
            : item == InventoryItemId.Shovel ? "SHOVEL"
            : item == InventoryItemId.Wood ? "WOOD"
            : item == InventoryItemId.Gold ? "GOLD" : "ITEM";

    public static bool TrySpendGold(int amount, out int remainingGold)
    {
        remainingGold = GetSecuredGoldTotal();
        if (amount < 0 || remainingGold < amount) return false;

        int playerSpend = Mathf.Min(amount, GetSecuredGoldTotal(Container.PlayerInventory));
        int chestSpend = amount - playerSpend;
        if (playerSpend > 0)
            RemoveAmount(Container.PlayerInventory, InventoryItemId.Gold, playerSpend);
        if (chestSpend > 0)
            RemoveAmount(Container.HomeChest, InventoryItemId.Gold, chestSpend);

        remainingGold = GetSecuredGoldTotal();
        return true;
    }

    public static void RemoveAll(InventoryItemId item)
    {
        EnsureLoaded();
        ClearItem(playerSlots, item);
        ClearItem(chestSlots, item);
        SaveAll();
    }

    public static void EnsureStarterAxe()
    {
        EnsureLoaded();
        if (FindItemSlot(Container.PlayerInventory, InventoryItemId.Axe) >= 0
            || FindItemSlot(Container.HomeChest, InventoryItemId.Axe) >= 0) return;
        AddItem(Container.HomeChest, InventoryItemId.Axe, 1);
    }

    public static void SetStarterToolsForNewGame()
    {
        var starters = new ItemStack[PlayerSlotCount];
        starters[0] = new ItemStack(InventoryItemId.Shovel, 1);
        starters[1] = new ItemStack(InventoryItemId.Axe, 1);
        starters[2] = new ItemStack(InventoryItemId.Pickaxe, 1);
        starters[3] = new ItemStack(InventoryItemId.Sword, 1);
        WriteBothSlots(starters, new ItemStack[ChestSlotCount]);
    }

    public static void ResetSavedState()
    {
        playerSlots.Clear();
        chestSlots.Clear();
        loaded = false;
        DeleteSlotKeys(Container.PlayerInventory);
        DeleteSlotKeys(Container.HomeChest);
        PlayerPrefs.DeleteKey(VersionKey);
        DeleteLegacyKeys();
    }

    static List<ItemStack> Slots(Container container)
        => container == Container.PlayerInventory ? playerSlots : chestSlots;

    static int SlotCount(Container container)
        => container == Container.PlayerInventory ? PlayerSlotCount : ChestSlotCount;

    static int ClampSlot(Container container, int slot)
        => Mathf.Clamp(slot, 0, SlotCount(container) - 1);

    static void InitializeSlots(List<ItemStack> slots, int count)
    {
        slots.Clear();
        for (int i = 0; i < count; i++) slots.Add(new ItemStack());
    }

    static void LoadSlots(Container container, List<ItemStack> slots, bool hasSecurityMetadata)
    {
        for (int slot = 0; slot < slots.Count; slot++)
        {
            int item = PlayerPrefs.GetInt(SlotKey(container, slot, "Item"), 0);
            int amount = PlayerPrefs.GetInt(SlotKey(container, slot, "Amount"), 0);
            int unsecured = hasSecurityMetadata
                ? PlayerPrefs.GetInt(SlotKey(container, slot, "Unsecured"), 0) : 0;
            slots[slot] = new ItemStack((InventoryItemId)item, amount, unsecured);
        }
    }

    static void MigrateLegacyStores()
    {
        ImportLegacyGold();
        ImportLegacyWood();
        ImportLegacyAxe(Container.PlayerInventory, "Tool.Axe.PlayerSlot");
        ImportLegacyAxe(Container.HomeChest, "Tool.Axe.ChestSlot");
    }

    static void ImportLegacyGold()
    {
        bool hadStacks = PlayerPrefs.GetInt("Gold.StacksMigrated", 0) == 1;
        if (hadStacks)
        {
            for (int legacyContainerIndex = 0; legacyContainerIndex <= 1; legacyContainerIndex++)
            {
                var typedContainer = (Container)legacyContainerIndex;
                for (int legacySlotIndex = 0; legacySlotIndex < SlotCount(typedContainer); legacySlotIndex++)
                    ImportStack(typedContainer, legacySlotIndex, InventoryItemId.Gold,
                        PlayerPrefs.GetInt($"Gold.Stack.{legacyContainerIndex}.{legacySlotIndex}", 0));
            }
            return;
        }

        int amount = Mathf.Max(0, PlayerPrefs.GetInt("Town.Supplies", 0));
        if (amount <= 0) return;
        Container container = (Container)Mathf.Clamp(PlayerPrefs.GetInt("Gold.Container", 0), 0, 1);
        int slot = Mathf.Clamp(PlayerPrefs.GetInt("Gold.Slot", 0), 0, SlotCount(container) - 1);
        ImportStack(container, slot, InventoryItemId.Gold, amount);
    }

    static void ImportLegacyWood()
    {
        if (PlayerPrefs.GetInt("Wood.StacksMigrated", 0) == 1)
        {
            for (int container = 0; container <= 1; container++)
            {
                var typedContainer = (Container)container;
                for (int slot = 0; slot < SlotCount(typedContainer); slot++)
                    ImportStack(typedContainer, slot, InventoryItemId.Wood,
                        PlayerPrefs.GetInt($"Wood.Stack.{container}.{slot}", 0));
            }
            return;
        }

        int amount = Mathf.Max(0, PlayerPrefs.GetInt("Resource.Wood", 0));
        if (amount > 0) ImportIntoFirstAvailable(Container.PlayerInventory, InventoryItemId.Wood, amount);
    }

    static void ImportLegacyAxe(Container container, string key)
    {
        int slot = PlayerPrefs.GetInt(key, -1);
        if (slot >= 0) ImportStack(container, slot, InventoryItemId.Axe, 1);
    }

    static void ImportStack(Container container, int slot, InventoryItemId item, int amount)
    {
        if (amount <= 0 || slot < 0 || slot >= SlotCount(container)) return;
        if (IsEmpty(container, slot))
        {
            Slots(container)[slot] = new ItemStack(item, amount);
            return;
        }
        ImportIntoFirstAvailable(container, item, amount);
    }

    static void ImportIntoFirstAvailable(Container container, InventoryItemId item, int amount)
    {
        int slot = FindItemSlot(container, item);
        if (slot < 0) slot = FindEmptySlot(container);
        if (slot >= 0)
        {
            ItemStack current = Slots(container)[slot];
            Slots(container)[slot] = new ItemStack(item, current.amount + amount);
        }
    }

    static void ClearItem(List<ItemStack> slots, InventoryItemId item)
    {
        for (int slot = 0; slot < slots.Count; slot++)
            if (slots[slot].item == item) slots[slot] = new ItemStack();
    }

    static ItemStack Normalize(ItemStack stack, Container container)
        => stack.amount > 0 && stack.item != InventoryItemId.Empty
            ? new ItemStack(stack.item, IsTool(stack.item) ? 1 : stack.amount,
                container == Container.PlayerInventory ? stack.unsecuredAmount : 0)
            : new ItemStack();

    static string SlotKey(Container container, int slot, string suffix)
        => $"{SlotKeyPrefix}.{(int)container}.{slot}.{suffix}";

    static void SaveAll()
    {
        if (!loaded) return;
        SaveSlots(Container.PlayerInventory, playerSlots);
        SaveSlots(Container.HomeChest, chestSlots);
        PlayerPrefs.SetInt(VersionKey, CurrentVersion);
        PlayerPrefs.SetInt("Town.Supplies", GetSecuredGoldTotal());
        PlayerPrefs.SetInt("Resource.Wood", GetTotal(InventoryItemId.Wood));
        if (GameState.Active != null)
        {
            GameState.Active.gold = GetSecuredGoldTotal();
            GameState.Active.playerItems = playerSlots.ToArray();
            GameState.Active.chestItems = chestSlots.ToArray();
        }
        PlayerPrefs.Save();
    }

    static void SaveSlots(Container container, List<ItemStack> slots)
    {
        for (int slot = 0; slot < slots.Count; slot++)
        {
            ItemStack stack = slots[slot];
            PlayerPrefs.SetInt(SlotKey(container, slot, "Item"), (int)stack.item);
            PlayerPrefs.SetInt(SlotKey(container, slot, "Amount"), stack.amount);
            PlayerPrefs.SetInt(SlotKey(container, slot, "Unsecured"), stack.unsecuredAmount);
        }
    }

    static void DeleteSlotKeys(Container container)
    {
        for (int slot = 0; slot < SlotCount(container); slot++)
        {
            PlayerPrefs.DeleteKey(SlotKey(container, slot, "Item"));
            PlayerPrefs.DeleteKey(SlotKey(container, slot, "Amount"));
            PlayerPrefs.DeleteKey(SlotKey(container, slot, "Unsecured"));
        }
    }

    static void DeleteLegacyKeys()
    {
        string[] keys = { "Town.Supplies", "Gold.Container", "Gold.Slot", "Gold.StacksMigrated",
            "Resource.Wood", "Wood.StacksMigrated", "Tool.Axe.PlayerSlot", "Tool.Axe.ChestSlot",
            "Tool.StarterAxeInitialized" };
        foreach (string key in keys) PlayerPrefs.DeleteKey(key);
        for (int container = 0; container <= 1; container++)
        {
            for (int slot = 0; slot < 16; slot++)
            {
                PlayerPrefs.DeleteKey($"Gold.Stack.{container}.{slot}");
                PlayerPrefs.DeleteKey($"Wood.Stack.{container}.{slot}");
            }
        }
    }
}
