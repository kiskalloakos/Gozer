public sealed class HomeBed : TownInteractable
{
    public override string Prompt => "Click to sleep until 8:00 AM";
    public override void Interact()
    {
        if (VillageTime.Instance) VillageTime.Instance.Sleep();
    }
}
