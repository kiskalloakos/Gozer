using UnityEngine;

public class TownInteractable : MonoBehaviour
{
    public enum FacilityType { Home, Workbench, Storage }

    public FacilityType facility;
    public string displayName;

    public virtual string Prompt => $"[E]  Use {displayName}";

    public virtual void Interact()
    {
        if (!TownHubController.Instance) return;
        switch (facility)
        {
            case FacilityType.Home:
                TownHubController.Instance.ShowNotice("Home — rested, safe, and ready for the next expedition.");
                break;
            case FacilityType.Workbench:
                TownHubController.Instance.ShowNotice("Workbench — weapon crafting and repairs will live here.");
                break;
            case FacilityType.Storage:
                TownHubController.Instance.ShowNotice($"Storage — {TownHubController.Instance.Supplies} town supplies are available.");
                break;
        }
    }
}
