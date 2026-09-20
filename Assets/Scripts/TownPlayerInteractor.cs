using UnityEngine;

public class TownPlayerInteractor : MonoBehaviour
{
    private TownInteractable hovered;
    private ScenePortal hoveredPortal;
    private bool interactionPending;

    void OnGUI()
    {
        var currentEvent = Event.current;
        if (currentEvent == null) return;

        Vector2 screenPointer = new Vector2(currentEvent.mousePosition.x, Screen.height - currentEvent.mousePosition.y);
        var camera = Camera.main;
        if (!camera) return;
        Vector2 worldPointer = camera.ScreenToWorldPoint(screenPointer);

        hovered = FindHovered(worldPointer);
        hoveredPortal = hovered ? null : FindHoveredPortal(worldPointer);
        CursorClickFeedback.SetInteractiveHover(hovered || hoveredPortal);

        if (currentEvent.type != EventType.MouseDown || currentEvent.button != 0) return;
        if (interactionPending || (!hovered && !hoveredPortal)) return;

        CursorClickFeedback.Pulse();
        StartCoroutine(InteractAfterCursorFeedback(hovered, hoveredPortal));
    }

    void OnDisable()
    {
        CursorClickFeedback.SetInteractiveHover(false);
    }

    private System.Collections.IEnumerator InteractAfterCursorFeedback(
        TownInteractable interactable,
        ScenePortal portal)
    {
        interactionPending = true;
        yield return new WaitForSecondsRealtime(CursorClickFeedback.InteractionDelaySeconds);

        if (interactable)
            interactable.Interact();
        else if (portal)
            portal.Interact();

        interactionPending = false;
    }

    private TownInteractable FindHovered(Vector2 pointer)
    {
        foreach (var candidate in FindObjectsByType<TownInteractable>(FindObjectsSortMode.None))
        {
            foreach (var renderer in candidate.GetComponentsInChildren<SpriteRenderer>())
            {
                if (!renderer.enabled || !renderer.sprite) continue;
                var bounds = renderer.bounds;
                if (pointer.x >= bounds.min.x && pointer.x <= bounds.max.x &&
                    pointer.y >= bounds.min.y && pointer.y <= bounds.max.y)
                    return candidate;
            }
        }
        return null;
    }

    private ScenePortal FindHoveredPortal(Vector2 pointer)
    {
        foreach (var portal in FindObjectsByType<ScenePortal>(FindObjectsSortMode.None))
            if (portal.IsPointerOverArt(pointer)) return portal;
        return null;
    }
}
