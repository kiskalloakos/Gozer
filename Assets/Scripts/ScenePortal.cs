using UnityEngine;

public class ScenePortal : MonoBehaviour
{
    public string destinationScene;
    public string destinationSpawnId;
    public string prompt = "Click to travel";

    private bool hovered;
    private bool interactionPending;

    public void Interact()
    {
        if (destinationScene == "ExpeditionField" && TownHubController.Instance &&
            !TownHubController.Instance.CanBeginExpedition()) return;
        SceneTravel.Load(destinationScene, destinationSpawnId);
    }

    public bool IsPointerOverArt(Vector2 pointer)
    {
        foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
        {
            if (!renderer.enabled || !renderer.sprite) continue;
            var bounds = renderer.bounds;
            if (pointer.x >= bounds.min.x && pointer.x <= bounds.max.x &&
                pointer.y >= bounds.min.y && pointer.y <= bounds.max.y)
                return true;
        }
        return false;
    }

    void OnGUI()
    {
        // Town scenes route all hover/click handling through the town cursor
        // controller so buildings and portals cannot overwrite each other's prompt.
        if (TownHubController.Instance || HomeInteriorController.Instance) return;

        var currentEvent = Event.current;
        var camera = Camera.main;
        if (currentEvent == null || !camera) return;

        Vector2 screenPointer = new Vector2(currentEvent.mousePosition.x, Screen.height - currentEvent.mousePosition.y);
        hovered = IsPointerOverArt(camera.ScreenToWorldPoint(screenPointer));
        CursorClickFeedback.SetInteractiveHover(hovered);
        if (hovered && currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
        {
            if (!interactionPending)
            {
                CursorClickFeedback.Pulse();
                StartCoroutine(InteractAfterCursorFeedback());
            }
        }
    }

    void OnDisable()
    {
        CursorClickFeedback.SetInteractiveHover(false);
    }

    private System.Collections.IEnumerator InteractAfterCursorFeedback()
    {
        interactionPending = true;
        yield return new WaitForSecondsRealtime(CursorClickFeedback.InteractionDelaySeconds);
        Interact();
        interactionPending = false;
    }
}
