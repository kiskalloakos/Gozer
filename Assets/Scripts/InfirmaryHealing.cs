using UnityEngine;

public sealed class InfirmaryHealing : TownInteractable
{
    [Min(0)] public int treatmentCost = 3;

    public override string Prompt => $"Click for treatment — restore 5 hearts ({treatmentCost} Gold)";

    public override void Interact()
    {
        var message = TownHubController.ApplyTreatment(treatmentCost);
        if (HomeInteriorController.Instance)
            HomeInteriorController.Instance.ShowNotice(message, 4.5f);
    }
}
