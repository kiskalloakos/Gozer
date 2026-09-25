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
    public int Amount => heldSecuredAmount + heldCarriedAmount;
    public bool IsHolding => HeldItem != InventoryItemId.Empty && Amount > 0;

    ItemInventory.Container originContainer;
    int originSlot;
    int heldSecuredAmount;
    int heldCarriedAmount;

    public int GetGoldSlotAmount(ItemInventory.Container container, int slot)
        => ItemInventory.GetAmount(container, slot, InventoryItemId.Gold);

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
        heldSecuredAmount = 0;
        heldCarriedAmount = 0;

        ItemStack source = ItemInventory.GetStack(container, slot);
        int taken = Mathf.Min(source.amount, amount);
        if (item == InventoryItemId.Gold)
        {
            heldCarriedAmount = Mathf.Min(source.unsecuredAmount, taken);
            heldSecuredAmount = taken - heldCarriedAmount;
            int remainingUnsecured = source.unsecuredAmount - heldCarriedAmount;
            int remainingSecured = source.amount - source.unsecuredAmount - heldSecuredAmount;
            ItemInventory.SetStack(container, slot, item, remainingSecured + remainingUnsecured,
                remainingUnsecured);
        }
        else
        {
            heldSecuredAmount = taken;
            ItemInventory.SetStack(container, slot, item, source.amount - taken);
        }

        if (Amount <= 0) HeldItem = InventoryItemId.Empty;
        else heldCursors.Add(this);
    }

    bool PlaceAll(ItemInventory.Container container, int slot)
    {
        if (!IsHolding || !ItemInventory.CanPlace(container, slot, HeldItem)) return false;
        int unsecuredAmount = container == ItemInventory.Container.PlayerInventory
            ? heldCarriedAmount : 0;
        ItemInventory.AddToSlot(container, slot, HeldItem, Amount, unsecuredAmount);
        ClearHeld();
        return true;
    }

    bool PlaceOne(ItemInventory.Container container, int slot)
    {
        if (!IsHolding || !ItemInventory.CanPlace(container, slot, HeldItem)) return false;
        if (heldCarriedAmount > 0)
        {
            int unsecuredAmount = container == ItemInventory.Container.PlayerInventory ? 1 : 0;
            ItemInventory.AddToSlot(container, slot, HeldItem, 1, unsecuredAmount);
            heldCarriedAmount--;
        }
        else if (heldSecuredAmount > 0)
        {
            ItemInventory.AddToSlot(container, slot, HeldItem, 1);
            heldSecuredAmount--;
        }
        if (Amount <= 0) ClearHeld();
        return true;
    }

    void ClearHeld()
    {
        heldCursors.Remove(this);
        HeldItem = InventoryItemId.Empty;
        heldSecuredAmount = 0;
        heldCarriedAmount = 0;
    }
}
