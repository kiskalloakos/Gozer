using System;
using UnityEditor;
using UnityEngine;

public static class GoldStorageTest
{
    [MenuItem("RPG/Tests/Validate Gold Storage")]
    public static void Validate()
    {
        var gold = SavedInt.Capture(TownHubController.GoldKey);
        var container = SavedInt.Capture(GoldInventoryLocation.ContainerKey);
        var slot = SavedInt.Capture(GoldInventoryLocation.SlotKey);

        try
        {
            PlayerPrefs.SetInt(TownHubController.GoldKey, 10);
            GoldInventoryLocation.MoveTo(GoldInventoryLocation.Container.HomeChest, 7);

            if (!TownHubController.TrySpendGold(3, out int remainingGold) || remainingGold != 7)
                throw new Exception("Gold stored in the chest could not be spent.");
            if (GoldInventoryLocation.CurrentContainer != GoldInventoryLocation.Container.HomeChest ||
                GoldInventoryLocation.CurrentSlot != 7)
                throw new Exception("Spending Gold unexpectedly moved its stored stack.");

            GoldInventoryLocation.MoveTo(GoldInventoryLocation.Container.PlayerInventory, 12);
            if (GoldInventoryLocation.CurrentContainer != GoldInventoryLocation.Container.PlayerInventory ||
                GoldInventoryLocation.CurrentSlot != 12)
                throw new Exception("Gold inventory slot movement did not persist.");

            Debug.Log("RPG_GOLD_STORAGE_VALIDATION_SUCCESS");
        }
        finally
        {
            gold.Restore(TownHubController.GoldKey);
            container.Restore(GoldInventoryLocation.ContainerKey);
            slot.Restore(GoldInventoryLocation.SlotKey);
            PlayerPrefs.Save();
        }
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
