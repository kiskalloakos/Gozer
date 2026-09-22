using UnityEngine;

public class ScenePortal : MonoBehaviour
{
    public GameScene destinationScene;
    public SceneSpawnPoint destinationSpawn = SceneSpawnPoint.None;
    [Tooltip("Marks this portal as the expedition entry and enforces the village clock gate.")]
    public bool requiresExpeditionTime;
    public string prompt = "Click to travel";

    private bool hovered;
    private bool interactionPending;
    private Transform player;

    public void Interact()
    {
        if ((requiresExpeditionTime || destinationScene == GameScene.ExpeditionField) && VillageTime.Instance &&
            !VillageTime.Instance.CanEnterExpedition()) return;
        if ((requiresExpeditionTime || destinationScene == GameScene.ExpeditionField) && TownHubController.Instance &&
            !TownHubController.Instance.CanBeginExpedition()) return;
        SceneTravel.Load(destinationScene, destinationSpawn);
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
        if (!player)
        {
            var playerController = FindAnyObjectByType<TownPlayerController>();
            player = playerController ? playerController.transform : null;
        }

        hovered = IsPointerOverArt(camera.ScreenToWorldPoint(screenPointer));
        CursorClickFeedback.SetInteractiveHover(hovered);
        if (hovered && currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
        {
            if (!interactionPending && player && InteractionProximity.IsWithinRange(player, this))
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
        if (player && InteractionProximity.IsWithinRange(player, this))
            Interact();
        interactionPending = false;
    }
}

/// <summary>
/// Keeps accepted mouse clicks local to the player. One world unit is one
/// terrain tile, and distance is measured edge-to-edge between colliders so
/// large buildings remain usable from any nearby side.
/// </summary>
public static class InteractionProximity
{
    public const float MaximumDistance = 2f;

    public static bool IsWithinRange(Transform player, Component target)
    {
        if (!player || !target) return false;

        var playerColliders = player.GetComponentsInChildren<Collider2D>();
        var targetColliders = target.GetComponentsInChildren<Collider2D>();
        bool comparedColliders = false;

        foreach (var playerCollider in playerColliders)
        {
            if (!IsUsable(playerCollider)) continue;

            foreach (var targetCollider in targetColliders)
            {
                if (!IsUsable(targetCollider)) continue;
                comparedColliders = true;
                if (playerCollider.Distance(targetCollider).distance <= MaximumDistance)
                    return true;
            }
        }

        if (comparedColliders) return false;

        // Art-only interactables still get a sensible range check.
        foreach (var renderer in target.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!renderer.enabled || !renderer.sprite) continue;
            Vector2 nearestPoint = renderer.bounds.ClosestPoint(player.position);
            if (Vector2.Distance(player.position, nearestPoint) <= MaximumDistance)
                return true;
        }

        return Vector2.Distance(player.position, target.transform.position) <= MaximumDistance;
    }

    private static bool IsUsable(Collider2D collider)
        => collider && collider.enabled && collider.gameObject.activeInHierarchy;
}
