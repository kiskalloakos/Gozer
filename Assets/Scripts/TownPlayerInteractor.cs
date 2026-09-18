using UnityEngine;

public class TownPlayerInteractor : MonoBehaviour
{
    public float interactionRadius = 2.1f;
    private TownInteractable nearest;

    void Update()
    {
        nearest = FindNearest();
        if (TownHubController.Instance)
            TownHubController.Instance.SetNearbyPrompt(nearest ? nearest.Prompt : "");
        if (nearest && Input.GetKeyDown(KeyCode.E)) nearest.Interact();
    }

    private TownInteractable FindNearest()
    {
        TownInteractable result = null;
        float bestDistance = interactionRadius;
        foreach (var candidate in FindObjectsByType<TownInteractable>(FindObjectsSortMode.None))
        {
            var hitbox = candidate.GetComponent<Collider2D>();
            if (!hitbox) hitbox = candidate.GetComponentInChildren<Collider2D>();
            Vector2 nearestPoint = hitbox && hitbox.enabled
                ? hitbox.ClosestPoint(transform.position)
                : candidate.transform.position;
            float distance = Vector2.Distance(transform.position, nearestPoint);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                result = candidate;
            }
        }
        return result;
    }
}
