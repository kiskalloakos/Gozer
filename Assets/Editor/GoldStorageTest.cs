using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class GoldStorageTest
{
    [MenuItem("RPG/Tests/Validate Gold Storage")]
    public static void Validate()
    {
        var saved = CaptureAllGoldState();

        try
        {
            GoldInventoryLocation.ResetSavedState();
            PlayerPrefs.SetInt(TownHubController.GoldKey, 10);
            PlayerPrefs.SetInt(GoldInventoryLocation.ContainerKey,
                (int)GoldInventoryLocation.Container.HomeChest);
            PlayerPrefs.SetInt(GoldInventoryLocation.SlotKey, 7);

            Assert(GoldInventoryLocation.GetAmount(GoldInventoryLocation.Container.HomeChest, 7) == 10,
                "The legacy chest stack was not migrated.");

            var cursor = new GoldStackCursor();
            cursor.RightClick(GoldInventoryLocation.Container.HomeChest, 7);
            Assert(cursor.TotalAmount == 5
                && GoldInventoryLocation.GetAmount(GoldInventoryLocation.Container.HomeChest, 7) == 5,
                "Right-click did not pick up half the stack.");

            cursor.RightClick(GoldInventoryLocation.Container.PlayerInventory, 12);
            cursor.RightClick(GoldInventoryLocation.Container.PlayerInventory, 13);
            cursor.LeftClick(GoldInventoryLocation.Container.PlayerInventory, 14);
            Assert(GoldInventoryLocation.GetAmount(GoldInventoryLocation.Container.PlayerInventory, 12) == 1
                && GoldInventoryLocation.GetAmount(GoldInventoryLocation.Container.PlayerInventory, 13) == 1
                && GoldInventoryLocation.GetAmount(GoldInventoryLocation.Container.PlayerInventory, 14) == 3,
                "The split stack was not distributed across player slots.");

            int extractedSlot = GoldInventoryLocation.AddAsNewPlayerStack(6);
            Assert(extractedSlot >= 0
                && GoldInventoryLocation.GetAmount(GoldInventoryLocation.Container.PlayerInventory, extractedSlot) == 6
                && GoldInventoryLocation.GetAmount(GoldInventoryLocation.Container.HomeChest, 7) == 5,
                "Extracted Gold did not create a separate player-inventory stack.");

            Assert(TownHubController.TrySpendGold(3, out int remainingGold) && remainingGold == 13,
                "Gold split across inventory and chest stacks could not be spent.");

            Debug.Log("RPG_GOLD_STORAGE_VALIDATION_SUCCESS: migration, splitting, distribution, extraction, and spending validated.");
        }
        finally
        {
            GoldInventoryLocation.ResetSavedState();
            foreach (var entry in saved) entry.Value.Restore(entry.Key);
            PlayerPrefs.Save();
        }
    }

    static Dictionary<string, SavedInt> CaptureAllGoldState()
    {
        var saved = new Dictionary<string, SavedInt>
        {
            [TownHubController.GoldKey] = SavedInt.Capture(TownHubController.GoldKey),
            [GoldInventoryLocation.ContainerKey] = SavedInt.Capture(GoldInventoryLocation.ContainerKey),
            [GoldInventoryLocation.SlotKey] = SavedInt.Capture(GoldInventoryLocation.SlotKey),
            [GoldInventoryLocation.StackMigrationKey] = SavedInt.Capture(GoldInventoryLocation.StackMigrationKey)
        };
        for (int container = 0; container <= 1; container++)
        {
            var typedContainer = (GoldInventoryLocation.Container)container;
            int slotCount = typedContainer == GoldInventoryLocation.Container.PlayerInventory
                ? GoldInventoryLocation.PlayerSlotCount
                : GoldInventoryLocation.ChestSlotCount;
            for (int slot = 0; slot < slotCount; slot++)
            {
                string key = GoldInventoryLocation.StackKey(typedContainer, slot);
                saved[key] = SavedInt.Capture(key);
            }
        }
        return saved;
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    readonly struct SavedInt
    {
        readonly bool existed;
        readonly int value;

        SavedInt(bool existed, int value)
        {
            this.existed = existed;
            this.value = value;
        }

        public static SavedInt Capture(string key)
            => new SavedInt(PlayerPrefs.HasKey(key), PlayerPrefs.GetInt(key));

        public void Restore(string key)
        {
            if (existed) PlayerPrefs.SetInt(key, value);
            else PlayerPrefs.DeleteKey(key);
        }
    }
}
