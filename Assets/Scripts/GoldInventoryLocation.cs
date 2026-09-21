using UnityEngine;

/// <summary>Persistent, slot-based storage for secured Gold stacks.</summary>
public static class GoldInventoryLocation
{
    public enum Container
    {
        PlayerInventory = 0,
        HomeChest = 1
    }

    public const int PlayerSlotCount = 16;
    public const int ChestSlotCount = 16;

    // Legacy keys are retained so existing saves can be migrated without losing Gold.
    public const string ContainerKey = "Gold.Container";
    public const string SlotKey = "Gold.Slot";
    public const string StackMigrationKey = "Gold.StacksMigrated";
    const string StackKeyPrefix = "Gold.Stack";

    static bool migrationChecked;

    public static int GetAmount(Container container, int slot)
    {
        EnsureMigrated();
        return PlayerPrefs.GetInt(StackKey(container, ClampSlot(container, slot)), 0);
    }

    public static void SetAmount(Container container, int slot, int amount)
    {
        EnsureMigrated();
        SetAmountWithoutMigration(container, slot, amount);
        SaveAndSyncTotal();
    }

    public static void AddAmount(Container container, int slot, int amount)
    {
        if (amount <= 0) return;
        SetAmount(container, slot, GetAmount(container, slot) + amount);
    }

    public static int GetTotalGold()
    {
        EnsureMigrated();
        return CalculateTotal();
    }

    public static bool TrySpend(int amount, out int remainingGold)
    {
        EnsureMigrated();
        int total = CalculateTotal();
        if (amount < 0 || total < amount)
        {
            remainingGold = total;
            return false;
        }

        int leftToSpend = amount;
        SpendFrom(Container.PlayerInventory, ref leftToSpend);
        SpendFrom(Container.HomeChest, ref leftToSpend);
        SaveAndSyncTotal();
        remainingGold = CalculateTotal();
        return true;
    }

    /// <summary>Adds extracted Gold to a new player stack whenever an empty slot exists.</summary>
    public static int AddAsNewPlayerStack(int amount, int preferredSlot = -1)
    {
        if (amount <= 0) return -1;
        EnsureMigrated();

        int slot = preferredSlot >= 0 && preferredSlot < PlayerSlotCount
            && GetAmount(Container.PlayerInventory, preferredSlot) == 0
                ? preferredSlot
                : FindEmptySlot(Container.PlayerInventory);
        if (slot < 0) slot = 0;
        SetAmountWithoutMigration(Container.PlayerInventory, slot,
            GetAmount(Container.PlayerInventory, slot) + amount);
        SaveAndSyncTotal();
        return slot;
    }

    public static int FindEmptySlot(Container container)
    {
        int slotCount = SlotCount(container);
        for (int slot = 0; slot < slotCount; slot++)
            if (GetAmount(container, slot) == 0) return slot;
        return -1;
    }

    public static string StackKey(Container container, int slot)
        => $"{StackKeyPrefix}.{(int)container}.{ClampSlot(container, slot)}";

    public static void ResetSavedState()
    {
        for (int container = 0; container <= 1; container++)
        {
            var typedContainer = (Container)container;
            for (int slot = 0; slot < SlotCount(typedContainer); slot++)
                PlayerPrefs.DeleteKey(StackKey(typedContainer, slot));
        }
        PlayerPrefs.DeleteKey(StackMigrationKey);
        PlayerPrefs.DeleteKey(ContainerKey);
        PlayerPrefs.DeleteKey(SlotKey);
        migrationChecked = false;
    }

    static void EnsureMigrated()
    {
        if (migrationChecked) return;
        migrationChecked = true;
        if (PlayerPrefs.GetInt(StackMigrationKey, 0) == 1) return;

        int legacyGold = Mathf.Max(0,
            PlayerPrefs.GetInt(TownHubController.GoldKey, TownHubController.DefaultStartingGold));
        var legacyContainer = (Container)Mathf.Clamp(
            PlayerPrefs.GetInt(ContainerKey, (int)Container.PlayerInventory), 0, 1);
        int legacySlot = Mathf.Clamp(PlayerPrefs.GetInt(SlotKey, 0), 0,
            SlotCount(legacyContainer) - 1);
        if (legacyGold > 0)
            SetAmountWithoutMigration(legacyContainer, legacySlot, legacyGold);

        PlayerPrefs.SetInt(StackMigrationKey, 1);
        SaveAndSyncTotal();
    }

    static void SpendFrom(Container container, ref int leftToSpend)
    {
        for (int slot = 0; slot < SlotCount(container) && leftToSpend > 0; slot++)
        {
            int amount = PlayerPrefs.GetInt(StackKey(container, slot), 0);
            int spent = Mathf.Min(amount, leftToSpend);
            SetAmountWithoutMigration(container, slot, amount - spent);
            leftToSpend -= spent;
        }
    }

    static int CalculateTotal()
    {
        int total = 0;
        for (int container = 0; container <= 1; container++)
        {
            var typedContainer = (Container)container;
            for (int slot = 0; slot < SlotCount(typedContainer); slot++)
                total += Mathf.Max(0, PlayerPrefs.GetInt(StackKey(typedContainer, slot), 0));
        }
        return total;
    }

    static void SetAmountWithoutMigration(Container container, int slot, int amount)
    {
        string key = StackKey(container, slot);
        if (amount > 0) PlayerPrefs.SetInt(key, amount);
        else PlayerPrefs.DeleteKey(key);
    }

    static void SaveAndSyncTotal()
    {
        PlayerPrefs.SetInt(TownHubController.GoldKey, CalculateTotal());
        PlayerPrefs.Save();
    }

    static int ClampSlot(Container container, int slot)
        => Mathf.Clamp(slot, 0, SlotCount(container) - 1);

    static int SlotCount(Container container)
        => container == Container.PlayerInventory ? PlayerSlotCount : ChestSlotCount;
}

/// <summary>Shared Minecraft-style cursor behavior for Gold stacks.</summary>
public sealed class GoldStackCursor
{
    public int SecuredAmount { get; private set; }
    public int CarriedAmount { get; private set; }
    public int TotalAmount => SecuredAmount + CarriedAmount;
    public bool IsHolding => TotalAmount > 0;

    GoldInventoryLocation.Container originContainer;
    int originSlot;

    public int GetSlotAmount(GoldInventoryLocation.Container container, int slot, ExpeditionHUD hud = null)
        => GoldInventoryLocation.GetAmount(container, slot) + GetCarried(container, slot, hud);

    public void LeftClick(GoldInventoryLocation.Container container, int slot, ExpeditionHUD hud = null)
    {
        if (!IsHolding)
        {
            int amount = GetSlotAmount(container, slot, hud);
            if (amount > 0) Take(container, slot, amount, hud);
            return;
        }

        PlaceAll(container, slot, hud);
    }

    public void RightClick(GoldInventoryLocation.Container container, int slot, ExpeditionHUD hud = null)
    {
        if (!IsHolding)
        {
            int amount = GetSlotAmount(container, slot, hud);
            if (amount > 0) Take(container, slot, (amount + 1) / 2, hud);
            return;
        }

        PlaceOne(container, slot, hud);
    }

    public void ReturnHeld(ExpeditionHUD hud = null)
    {
        if (!IsHolding) return;
        PlaceAll(originContainer, originSlot, hud);
    }

    void Take(GoldInventoryLocation.Container container, int slot, int amount, ExpeditionHUD hud)
    {
        originContainer = container;
        originSlot = slot;

        int carriedAvailable = GetCarried(container, slot, hud);
        CarriedAmount = Mathf.Min(carriedAvailable, amount);
        if (CarriedAmount > 0) hud.SetCarriedLootAtSlot(slot, carriedAvailable - CarriedAmount);

        int securedToTake = amount - CarriedAmount;
        int securedAvailable = GoldInventoryLocation.GetAmount(container, slot);
        SecuredAmount = Mathf.Min(securedAvailable, securedToTake);
        if (SecuredAmount > 0)
            GoldInventoryLocation.SetAmount(container, slot, securedAvailable - SecuredAmount);
    }

    void PlaceAll(GoldInventoryLocation.Container container, int slot, ExpeditionHUD hud)
    {
        if (SecuredAmount > 0)
            GoldInventoryLocation.AddAmount(container, slot, SecuredAmount);
        if (CarriedAmount > 0 && container == GoldInventoryLocation.Container.PlayerInventory && hud)
            hud.SetCarriedLootAtSlot(slot, hud.GetCarriedLootAtSlot(slot) + CarriedAmount);
        else if (CarriedAmount > 0)
            GoldInventoryLocation.AddAmount(container, slot, CarriedAmount);

        SecuredAmount = 0;
        CarriedAmount = 0;
    }

    void PlaceOne(GoldInventoryLocation.Container container, int slot, ExpeditionHUD hud)
    {
        if (CarriedAmount > 0 && container == GoldInventoryLocation.Container.PlayerInventory && hud)
        {
            hud.SetCarriedLootAtSlot(slot, hud.GetCarriedLootAtSlot(slot) + 1);
            CarriedAmount--;
            return;
        }

        if (SecuredAmount > 0)
        {
            GoldInventoryLocation.AddAmount(container, slot, 1);
            SecuredAmount--;
            return;
        }

        if (CarriedAmount > 0)
        {
            GoldInventoryLocation.AddAmount(container, slot, 1);
            CarriedAmount--;
        }
    }

    static int GetCarried(GoldInventoryLocation.Container container, int slot, ExpeditionHUD hud)
        => container == GoldInventoryLocation.Container.PlayerInventory && hud
            ? hud.GetCarriedLootAtSlot(slot)
            : 0;
}
