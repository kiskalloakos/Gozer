using UnityEngine;

/// <summary>One cursor for moving any item stack between inventory panels.</summary>
public sealed class InventoryStackCursor
{
    public InventoryItemId HeldItem { get; private set; }
    public int Amount => heldSecuredAmount + heldCarriedAmount;
    public bool IsHolding => HeldItem != InventoryItemId.Empty && Amount > 0;

    ItemInventory.Container originContainer;
    int originSlot;
    int heldSecuredAmount;
    int heldCarriedAmount;

    public int GetGoldSlotAmount(ItemInventory.Container container, int slot)
    {
        int amount = ItemInventory.GetAmount(container, slot, InventoryItemId.Gold);
        if (container == ItemInventory.Container.PlayerInventory)
            amount += CurrentExpeditionLoot.GetAtSlot(slot);
        return amount;
    }

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
        if (stack.item != InventoryItemId.Empty && stack.amount > 0)
        {
            item = stack.item;
            amount = stack.amount;
            if (item == InventoryItemId.Gold && container == ItemInventory.Container.PlayerInventory)
                amount += CurrentExpeditionLoot.GetAtSlot(slot);
            return true;
        }

        if (container == ItemInventory.Container.PlayerInventory
            && CurrentExpeditionLoot.GetAtSlot(slot) > 0)
        {
            item = InventoryItemId.Gold;
            amount = CurrentExpeditionLoot.GetAtSlot(slot);
            return true;
        }

        item = InventoryItemId.Empty;
        amount = 0;
        return false;
    }

    void Take(ItemInventory.Container container, int slot, InventoryItemId item, int amount)
    {
        originContainer = container;
        originSlot = slot;
        HeldItem = item;
        heldSecuredAmount = 0;
        heldCarriedAmount = 0;

        if (item == InventoryItemId.Gold)
        {
            int securedAvailable = ItemInventory.GetAmount(container, slot, item);
            int carriedAvailable = container == ItemInventory.Container.PlayerInventory
                ? CurrentExpeditionLoot.GetAtSlot(slot) : 0;
            int remaining = Mathf.Min(amount, securedAvailable + carriedAvailable);
            heldCarriedAmount = Mathf.Min(carriedAvailable, remaining);
            heldSecuredAmount = Mathf.Min(securedAvailable, remaining - heldCarriedAmount);
            if (heldCarriedAmount > 0)
                CurrentExpeditionLoot.SetAtSlot(slot, carriedAvailable - heldCarriedAmount);
            if (heldSecuredAmount > 0)
                ItemInventory.SetStack(container, slot, item, securedAvailable - heldSecuredAmount);
        }
        else
        {
            ItemStack stack = ItemInventory.GetStack(container, slot);
            heldSecuredAmount = Mathf.Min(stack.amount, amount);
            ItemInventory.SetStack(container, slot, item, stack.amount - heldSecuredAmount);
        }

        if (Amount <= 0) HeldItem = InventoryItemId.Empty;
    }

    bool PlaceAll(ItemInventory.Container container, int slot)
    {
        if (!IsHolding || !ItemInventory.CanPlace(container, slot, HeldItem)) return false;
        if (heldSecuredAmount > 0)
            ItemInventory.AddToSlot(container, slot, HeldItem, heldSecuredAmount);
        if (heldCarriedAmount > 0)
        {
            if (container == ItemInventory.Container.PlayerInventory)
                CurrentExpeditionLoot.SetAtSlot(slot, CurrentExpeditionLoot.GetAtSlot(slot) + heldCarriedAmount);
            else
                ItemInventory.AddToSlot(container, slot, HeldItem, heldCarriedAmount);
        }
        ClearHeld();
        return true;
    }

    bool PlaceOne(ItemInventory.Container container, int slot)
    {
        if (!IsHolding || !ItemInventory.CanPlace(container, slot, HeldItem)) return false;
        if (heldCarriedAmount > 0)
        {
            if (container == ItemInventory.Container.PlayerInventory)
                CurrentExpeditionLoot.SetAtSlot(slot, CurrentExpeditionLoot.GetAtSlot(slot) + 1);
            else
                ItemInventory.AddToSlot(container, slot, HeldItem, 1);
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
        HeldItem = InventoryItemId.Empty;
        heldSecuredAmount = 0;
        heldCarriedAmount = 0;
    }
}
