using UnityEngine;

/// <summary>Operations on Gold stacks that are still unsecured during a run.</summary>
public static class CurrentExpeditionLoot
{
    static ItemStack[] PlayerStacks
    {
        get
        {
            GameState.InstallFromRuntime();
            return ItemInventory.ReadSlots(ItemInventory.Container.PlayerInventory);
        }
    }

    public static int Total
    {
        get
        {
            int total = 0;
            foreach (ItemStack stack in PlayerStacks) total += stack.unsecuredAmount;
            return total;
        }
    }

    public static bool HasAny => Total > 0;

    public static int GetAtSlot(int slot)
        => slot >= 0 && slot < ItemInventory.PlayerSlotCount
            ? PlayerStacks[slot].unsecuredAmount : 0;

    public static int FindSlotForAdd()
    {
        int goldSlot = ItemInventory.FindItemSlot(ItemInventory.Container.PlayerInventory, InventoryItemId.Gold);
        return goldSlot >= 0
            ? goldSlot
            : ItemInventory.FindEmptySlot(ItemInventory.Container.PlayerInventory);
    }

    public static void SetAtSlot(int slot, int amount)
    {
        if (slot < 0 || slot >= ItemInventory.PlayerSlotCount) return;
        ItemStack stack = ItemInventory.GetStack(ItemInventory.Container.PlayerInventory, slot);
        if (stack.item != InventoryItemId.Empty && stack.item != InventoryItemId.Gold) return;
        int unsecured = Mathf.Max(0, amount);
        int secured = stack.amount - stack.unsecuredAmount;
        int total;
        if (stack.item == InventoryItemId.Empty)
        {
            total = unsecured;
        }
        else total = secured + unsecured;
        stack = new ItemStack(InventoryItemId.Gold, total, unsecured);
        ItemInventory.SetStack(ItemInventory.Container.PlayerInventory, slot,
            stack.item, stack.amount, stack.unsecuredAmount);
    }

    public static bool Add(int amount, int slot)
    {
        if (amount <= 0 || slot < 0 || slot >= ItemInventory.PlayerSlotCount
            || !ItemInventory.CanPlace(ItemInventory.Container.PlayerInventory, slot, InventoryItemId.Gold)) return false;
        return ItemInventory.AddPickedUpItem(ItemInventory.Container.PlayerInventory,
            InventoryItemId.Gold, amount, slot, amount);
    }

    public static int Secure()
    {
        int total = Total;
        if (total <= 0) return 0;

        ItemStack[] slots = PlayerStacks;
        for (int slot = 0; slot < slots.Length; slot++)
        {
            if (slots[slot].unsecuredAmount <= 0) continue;
            slots[slot] = new ItemStack(slots[slot].item, slots[slot].amount);
        }
        ItemInventory.WriteSlots(ItemInventory.Container.PlayerInventory, slots);
        GameState.InstallFromRuntime();
        GameState.Active.pendingSecuredGold += total;
        GameState.Active.gold = ItemInventory.GetSecuredGoldTotal();
        return total;
    }

    public static void Lose()
    {
        ItemStack[] slots = PlayerStacks;
        bool changed = false;
        for (int slot = 0; slot < slots.Length; slot++)
        {
            ItemStack stack = slots[slot];
            if (stack.unsecuredAmount <= 0) continue;
            slots[slot] = new ItemStack(stack.item, stack.amount - stack.unsecuredAmount);
            changed = true;
        }
        if (changed) ItemInventory.WriteSlots(ItemInventory.Container.PlayerInventory, slots);
    }

    /// <summary>Migrate the old parallel loot array once, then discard it.</summary>
    public static void MigrateLegacyState(GameState state)
    {
        if (state == null || state.expeditionLoot == null) return;
        if (state.expeditionLoot.Length == 0)
        {
            state.expeditionLoot = null;
            return;
        }

        ItemStack[] slots = ItemInventory.ReadSlots(ItemInventory.Container.PlayerInventory);
        bool changed = false;
        int pending = 0;
        for (int slot = 0; slot < state.expeditionLoot.Length; slot++)
        {
            int amount = Mathf.Max(0, state.expeditionLoot[slot]);
            if (amount <= 0) continue;
            if (slot < slots.Length && (slots[slot].item == InventoryItemId.Empty
                    || slots[slot].item == InventoryItemId.Gold))
            {
                slots[slot] = new ItemStack(InventoryItemId.Gold,
                    slots[slot].amount + amount, slots[slot].unsecuredAmount + amount);
                changed = true;
                continue;
            }

            int goldSlot = -1;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].item == InventoryItemId.Gold) { goldSlot = i; break; }
            if (goldSlot < 0)
                for (int i = 0; i < slots.Length; i++)
                    if (slots[i].item == InventoryItemId.Empty) { goldSlot = i; break; }
            if (goldSlot < 0) { pending += amount; continue; }

            slots[goldSlot] = new ItemStack(InventoryItemId.Gold,
                slots[goldSlot].amount + amount, slots[goldSlot].unsecuredAmount + amount);
            changed = true;
        }

        state.expeditionLoot = pending > 0 ? new[] { pending } : null;
        if (changed) ItemInventory.WriteSlots(ItemInventory.Container.PlayerInventory, slots);
        if (pending > 0)
            Debug.LogWarning($"Could not migrate {pending} legacy expedition Gold because the player inventory is full.");
    }

    public static void ResetSavedState()
    {
        if (GameState.Active != null) GameState.Active.expeditionLoot = null;
        PlayerPrefs.DeleteKey("Expedition.CurrentLoot.v1");
    }
}
