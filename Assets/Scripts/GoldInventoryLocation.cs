using UnityEngine;

public static class GoldInventoryLocation
{
    public enum Container
    {
        PlayerInventory = 0,
        HomeChest = 1
    }

    // The full player bag is a 4 x 4 grid. The bottom quickbar presents its first four slots.
    public const int PlayerSlotCount = 16;
    public const int ChestSlotCount = 16;
    public const string ContainerKey = "Gold.Container";
    public const string SlotKey = "Gold.Slot";

    public static Container CurrentContainer => (Container)Mathf.Clamp(
        PlayerPrefs.GetInt(ContainerKey, (int)Container.PlayerInventory),
        (int)Container.PlayerInventory,
        (int)Container.HomeChest);

    public static int CurrentSlot
    {
        get
        {
            int slotCount = CurrentContainer == Container.PlayerInventory
                ? PlayerSlotCount
                : ChestSlotCount;
            return Mathf.Clamp(PlayerPrefs.GetInt(SlotKey, 0), 0, slotCount - 1);
        }
    }

    public static void MoveTo(Container container, int slot)
    {
        int slotCount = container == Container.PlayerInventory
            ? PlayerSlotCount
            : ChestSlotCount;
        PlayerPrefs.SetInt(ContainerKey, (int)container);
        PlayerPrefs.SetInt(SlotKey, Mathf.Clamp(slot, 0, slotCount - 1));
        PlayerPrefs.Save();
    }
}
