using System;
using UnityEngine;

/// <summary>Unsecured Gold gathered during the current expedition.</summary>
public static class CurrentExpeditionLoot
{
    static int[] Slots
    {
        get
        {
            GameState.InstallFromRuntime();
            if (GameState.Active.expeditionLoot == null || GameState.Active.expeditionLoot.Length != ItemInventory.PlayerSlotCount)
                GameState.Active.expeditionLoot = new int[ItemInventory.PlayerSlotCount];
            return GameState.Active.expeditionLoot;
        }
    }

    public static int Total
    {
        get { int total = 0; foreach (int amount in Slots) total += amount; return total; }
    }

    public static bool HasAny
    {
        get { foreach (int amount in Slots) if (amount > 0) return true; return false; }
    }

    public static int GetAtSlot(int slot)
    {
        int[] slots = Slots;
        return slot >= 0 && slot < slots.Length ? slots[slot] : 0;
    }

    public static int FindSlotForAdd()
    {
        int[] slots = Slots;
        for (int slot = 0; slot < slots.Length; slot++) if (slots[slot] > 0) return slot;
        for (int slot = 0; slot < slots.Length; slot++)
            if (ItemInventory.GetAmount(ItemInventory.Container.PlayerInventory, slot, InventoryItemId.Gold) > 0) return slot;
        for (int slot = 0; slot < slots.Length; slot++)
            if (ItemInventory.IsEmpty(ItemInventory.Container.PlayerInventory, slot)) return slot;
        return 0;
    }

    public static void SetAtSlot(int slot, int amount)
    {
        int[] slots = Slots;
        if (slot < 0 || slot >= slots.Length) return;
        slots[slot] = Mathf.Max(0, amount);
    }

    public static void Add(int amount)
    {
        if (amount <= 0) return;
        int[] slots = Slots;
        int slot = FindSlotForAdd();
        slots[slot] += amount;
    }

    public static int Secure()
    {
        int[] slots = Slots;
        int total = Total;
        if (total <= 0) return 0;
        ItemStack[] player = ItemInventory.ReadSlots(ItemInventory.Container.PlayerInventory);
        ItemStack[] chest = ItemInventory.ReadSlots(ItemInventory.Container.HomeChest);
        for (int slot = 0; slot < slots.Length; slot++)
        {
            int amount = slots[slot];
            if (amount <= 0) continue;
            bool stored = TryAdd(player, slot, InventoryItemId.Gold, amount);
            if (!stored) stored = TryAdd(player, -1, InventoryItemId.Gold, amount);
            if (!stored) stored = TryAdd(chest, -1, InventoryItemId.Gold, amount);
            if (!stored) return 0;
        }
        // Persist loot removal and secured inventory together. On reload, either
        // the old run state or the complete secured state is visible.
        ItemInventory.WriteBothSlots(player, chest);
        Array.Clear(slots, 0, slots.Length);
        GameState.Active.pendingSecuredGold += total;
        GameState.Active.gold = ItemInventory.GetTotal(InventoryItemId.Gold);
        return total;
    }

    static bool TryAdd(ItemStack[] target, int slot, InventoryItemId item, int amount)
    {
        if (slot >= 0 && slot < target.Length)
        {
            if (target[slot].item != InventoryItemId.Empty && target[slot].item != item) return false;
            target[slot] = new ItemStack(item, target[slot].amount + amount);
            return true;
        }
        for (int i = 0; i < target.Length; i++)
            if (target[i].item == item) { target[i] = new ItemStack(item, target[i].amount + amount); return true; }
        for (int i = 0; i < target.Length; i++)
            if (target[i].item == InventoryItemId.Empty) { target[i] = new ItemStack(item, amount); return true; }
        return false;
    }

    public static void Lose() => Array.Clear(Slots, 0, Slots.Length);

    public static void ResetSavedState()
    {
        Array.Clear(Slots, 0, Slots.Length);
        PlayerPrefs.DeleteKey("Expedition.CurrentLoot.v1");
    }
}
