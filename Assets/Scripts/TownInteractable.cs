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
                SceneTravel.Load(GameScene.HomeInterior);
                break;
            case FacilityType.Workbench:
                WorkbenchCraftingUI.OpenFor(this);
                break;
            case FacilityType.Storage:
                if (TownHubController.Instance)
                    TownHubController.Instance.ShowNotice($"Storage — {TownHubController.Instance.Gold} Gold is available.");
                break;
        }
    }

}
