using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class ExpeditionLootPickup : MonoBehaviour
{
    const float WorldVisualScale = .35f;
    const float AttractionRadius = 1.4f;
    const float CollectionDuration = .32f;
    const float PlayerBodyYOffset = .15f;

    [Min(1)] public int amount = 1;

    SpriteRenderer visual;
    GoldVisualAssets visualAssets;
    float animationStartedAt;
    ExpeditionHUD collectingInventory;
    Transform collectionTarget;
    Vector3 collectionStart;
    Vector3 visualStartScale;
    float collectionStartedAt;
    Transform blockedPlayer;

    const float RetryDistancePadding = 1f;

    public static void Spawn(Vector2 position, int amount)
    {
        if (amount <= 0) return;

        var pickup = new GameObject("Dropped Gold");
        pickup.transform.position = position;

        var trigger = pickup.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = AttractionRadius;

        var visualObject = new GameObject("Visual");
        visualObject.transform.SetParent(pickup.transform, false);
        visualObject.transform.localScale = Vector3.one * WorldVisualScale;
        var renderer = visualObject.AddComponent<SpriteRenderer>();
        GoldVisualAssets assets = GoldVisualAssets.Load();
        if (assets && assets.floatingFrames != null && assets.floatingFrames.Length > 0)
            renderer.sprite = assets.floatingFrames[0];
        renderer.sortingOrder = Mathf.RoundToInt(-position.y * 100) + 1;

        var loot = pickup.AddComponent<ExpeditionLootPickup>();
        loot.amount = amount;
    }

    void Awake()
    {
        visual = GetComponentInChildren<SpriteRenderer>();
        visualAssets = GoldVisualAssets.Load();
        animationStartedAt = Time.time;
    }

    void Update()
    {
        AnimateFloatingGold();
        if (blockedPlayer)
        {
            float retryDistance = AttractionRadius + RetryDistancePadding;
            if (((Vector2)(blockedPlayer.position - transform.position)).sqrMagnitude
                > retryDistance * retryDistance)
            {
                blockedPlayer = null;
                var blockedTrigger = GetComponent<CircleCollider2D>();
                if (blockedTrigger) blockedTrigger.enabled = true;
            }
        }
        if (collectionTarget) AnimateCollection();
    }

    void AnimateFloatingGold()
    {
        if (!visual || !visualAssets || visualAssets.floatingFrames == null ||
            visualAssets.floatingFrames.Length == 0) return;
        int frame = Mathf.FloorToInt((Time.time - animationStartedAt) * visualAssets.framesPerSecond)
            % visualAssets.floatingFrames.Length;
        visual.sprite = visualAssets.floatingFrames[frame];
    }

    void AnimateCollection()
    {
        float progress = Mathf.Clamp01((Time.time - collectionStartedAt) / CollectionDuration);
        float eased = progress * progress * (3f - 2f * progress);
        Vector3 targetPosition = collectionTarget.position + Vector3.up * PlayerBodyYOffset;
        transform.position = Vector3.Lerp(collectionStart, targetPosition, eased);
        if (visual)
            visual.transform.localScale = Vector3.Lerp(visualStartScale, visualStartScale * .2f, eased);

        if (progress < 1f) return;
        if (!collectingInventory || !collectingInventory.AddLoot(amount))
        {
            if (collectingInventory) collectingInventory.ShowStatus("GOLD BALANCE FULL");
            transform.position = collectionStart;
            if (visual) visual.transform.localScale = visualStartScale;
            blockedPlayer = collectionTarget;
            collectingInventory = null;
            collectionTarget = null;
            return;
        }
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other) => TryCollect(other);
    void OnTriggerStay2D(Collider2D other) => TryCollect(other);

    void TryCollect(Collider2D other)
    {
        if (collectionTarget || blockedPlayer) return;

        var player = other.GetComponentInParent<ExpeditionPlayerHealth>();
        if (!player) return;
        var inventory = player.GetComponent<ExpeditionHUD>();
        if (!inventory) return;

        collectingInventory = inventory;
        collectionTarget = player.transform;
        collectionStart = transform.position;
        visualStartScale = visual ? visual.transform.localScale : Vector3.one;
        collectionStartedAt = Time.time;

        var trigger = GetComponent<CircleCollider2D>();
        if (trigger) trigger.enabled = false;
    }
}
