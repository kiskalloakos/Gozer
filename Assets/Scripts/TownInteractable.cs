using UnityEngine;

public class TownInteractable : MonoBehaviour
{
    public enum FacilityType { Home, Workbench, Storage }

    public FacilityType facility;
    public string displayName;

    public virtual string Prompt => facility == FacilityType.Home
        ? $"Click to enter {displayName}"
        : $"Click to use {displayName}";

    public virtual void Interact()
    {
        switch (facility)
        {
            case FacilityType.Home:
                SceneTravel.Load("HomeInterior");
                break;
            case FacilityType.Workbench:
                PurchaseReinforcedMelee();
                break;
            case FacilityType.Storage:
                if (TownHubController.Instance)
                    TownHubController.Instance.ShowNotice($"Storage — {TownHubController.Instance.Gold} Gold is available.");
                break;
        }
    }

    static void PurchaseReinforcedMelee()
    {
        var result = PlayerProgression.PurchaseReinforcedMelee(out int remainingGold);
        switch (result)
        {
            case PlayerProgression.PurchaseResult.Purchased:
                ShowNotice($"Reinforced melee weapon installed — damage increased to {PlayerProgression.ReinforcedMeleeDamage}. {remainingGold} Gold remains.", 5f);
                break;
            case PlayerProgression.PurchaseResult.NotEnoughGold:
                ShowNotice($"Reinforced melee weapon costs {PlayerProgression.ReinforcedMeleeCost} Gold. You currently have {remainingGold}.", 5f);
                break;
            case PlayerProgression.PurchaseResult.AlreadyOwned:
                ShowNotice("Reinforced melee weapon already installed — damage is permanently increased to 2.", 4.5f);
                break;
        }
    }

    static void ShowNotice(string message, float seconds = 3.5f)
    {
        if (TownHubController.Instance)
            TownHubController.Instance.ShowNotice(message, seconds);
        else if (HomeInteriorController.Instance)
            HomeInteriorController.Instance.ShowNotice(message, seconds);
    }
}
