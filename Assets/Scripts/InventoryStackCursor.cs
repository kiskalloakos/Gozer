using System.Collections.Generic;
using UnityEngine;

/// <summary>One cursor for moving any item stack between inventory panels.</summary>
public sealed class InventoryStackCursor
{
    static readonly HashSet<InventoryStackCursor> heldCursors = new HashSet<InventoryStackCursor>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetHeldCursors() => heldCursors.Clear();

    public static bool IsReserved(ItemInventory.Container container, int slot)
    {
        foreach (var cursor in heldCursors)
            if (cursor.IsHolding && cursor.originContainer == container && cursor.originSlot == slot)
                return true;
        return false;
    }

    public static bool ReturnAllHeld()
    {
        var cursors = new List<InventoryStackCursor>(heldCursors);
        foreach (var cursor in cursors) cursor.ReturnHeld();
        return heldCursors.Count == 0;
    }

    public InventoryItemId HeldItem { get; private set; }
    public int Amount => heldAmount;
    public bool IsHolding => HeldItem != InventoryItemId.Empty && Amount > 0;

    ItemInventory.Container originContainer;
    int originSlot;
    int heldAmount;

    public void LeftClick(ItemInventory.Container container, int slot)
    {
        if (!IsHolding)
        {
            if (TryGetAvailable(container, slot, out InventoryItemId item, out int amount))
                Take(container, slot, item, amount);
            return;
        }

        PlaceAll(container, slot);
    }

    public void RightClick(ItemInventory.Container container, int slot)
    {
        if (!IsHolding)
        {
            if (TryGetAvailable(container, slot, out InventoryItemId item, out int amount))
                Take(container, slot, item, (amount + 1) / 2);
            return;
        }

        PlaceOne(container, slot);
    }

    public void ReturnHeld()
    {
        if (!IsHolding) return;
        if (PlaceAll(originContainer, originSlot)) return;

        int emptySlot = ItemInventory.FindEmptySlot(originContainer);
        if (emptySlot >= 0) PlaceAll(originContainer, emptySlot);
    }

    bool TryGetAvailable(ItemInventory.Container container, int slot,
        out InventoryItemId item, out int amount)
    {
        ItemStack stack = ItemInventory.GetStack(container, slot);
        item = stack.item;
        amount = stack.amount;
        return item != InventoryItemId.Empty && amount > 0;
    }

    void Take(ItemInventory.Container container, int slot, InventoryItemId item, int amount)
    {
        originContainer = container;
        originSlot = slot;
        HeldItem = item;
        ItemStack source = ItemInventory.GetStack(container, slot);
        heldAmount = Mathf.Min(source.amount, amount);
        ItemInventory.SetStack(container, slot, item, source.amount - heldAmount);

        if (Amount <= 0) HeldItem = InventoryItemId.Empty;
        else heldCursors.Add(this);
    }

    bool PlaceAll(ItemInventory.Container container, int slot)
    {
        if (!IsHolding || !ItemInventory.CanPlace(container, slot, HeldItem)) return false;
        ItemInventory.AddToSlot(container, slot, HeldItem, Amount);
        ClearHeld();
        return true;
    }

    bool PlaceOne(ItemInventory.Container container, int slot)
    {
        if (!IsHolding || !ItemInventory.CanPlace(container, slot, HeldItem)) return false;
        ItemInventory.AddToSlot(container, slot, HeldItem, 1);
        heldAmount--;
        if (Amount <= 0) ClearHeld();
        return true;
    }

    void ClearHeld()
    {
        heldCursors.Remove(this);
        HeldItem = InventoryItemId.Empty;
        heldAmount = 0;
    }
}
