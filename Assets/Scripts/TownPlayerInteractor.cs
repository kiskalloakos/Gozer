using System.Collections.Generic;
using UnityEngine;

public class TownPlayerInteractor : MonoBehaviour
{
    private TownInteractable hovered;
    private ScenePortal hoveredPortal;
    private bool interactionPending;
    TownInteractable[] interactables;
    ScenePortal[] portals;
    readonly Dictionary<TownInteractable, SpriteRenderer[]> artwork =
        new Dictionary<TownInteractable, SpriteRenderer[]>();

    void Start()
    {
        interactables = FindObjectsByType<TownInteractable>();
        portals = FindObjectsByType<ScenePortal>();
        artwork.Clear();
        foreach (var interactable in interactables)
            if (interactable) artwork[interactable] = interactable.GetComponentsInChildren<SpriteRenderer>();
    }

    void OnGUI()
    {
        if (GameSessionFlow.IsBlockingGameplay) return;
        if (VillageTime.Instance && VillageTime.Instance.IsSleeping) return;
        if (HomeStorageChest.IsModalOpen || WorkbenchCraftingUI.IsModalOpen || PlayerInventoryUI.IsOpen)
        {
            CursorClickFeedback.SetInteractiveHover(false);
            return;
        }

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
        if (hovered && !InteractionProximity.IsWithinRange(transform, hovered)) return;
        if (hoveredPortal && !InteractionProximity.IsWithinRange(transform, hoveredPortal)) return;

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

        if (GameSessionFlow.IsBlockingGameplay || (VillageTime.Instance && VillageTime.Instance.IsSleeping))
        {
            interactionPending = false;
            yield break;
        }

        if (interactable && InteractionProximity.IsWithinRange(transform, interactable))
            interactable.Interact();
        else if (portal && InteractionProximity.IsWithinRange(transform, portal))
            portal.Interact();

        interactionPending = false;
    }

    private TownInteractable FindHovered(Vector2 pointer)
    {
        if (interactables == null) return null;
        foreach (var candidate in interactables)
        {
            if (!candidate || !candidate.isActiveAndEnabled) continue;
            foreach (var renderer in artwork[candidate])
            {
                if (!renderer || !renderer.enabled || !renderer.sprite) continue;
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
        if (portals == null) return null;
        foreach (var portal in portals)
            if (portal && portal.isActiveAndEnabled && portal.IsPointerOverArt(pointer)) return portal;
        return null;
    }
}
