using UnityEngine;

public class TownInteractable : MonoBehaviour
{
    public enum FacilityType { Home, Workbench, Storage, Infirmary }

    public FacilityType facility;
    public string displayName;
    public Collider2D interactionCollider;

    public virtual string Prompt => facility == FacilityType.Home || facility == FacilityType.Infirmary
        ? $"Click to enter {displayName}"
        : $"Click to use {displayName}";

    public virtual void Interact()
    {
        switch (facility)
        {
            case FacilityType.Home:
                SceneTravel.Load(GameScene.HomeInterior);
                break;
            case FacilityType.Infirmary:
                SceneTravel.Load(GameScene.InfirmaryInterior);
                break;
            case FacilityType.Workbench:
                WorkbenchCraftingUI.OpenFor(this);
                break;
            case FacilityType.Storage:
                if (TownHubController.Instance)
                    TownHubController.Instance.ShowNotice("Storage is available at home.");
                break;
        }
    }

}
