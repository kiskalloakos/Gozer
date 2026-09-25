using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public sealed class WoodPickup : MonoBehaviour
{
    const float WorldVisualScale = .35f;
    // Wood should remain on the ground until the player deliberately walks
    // over it. Gold uses a larger magnet radius, but chopped wood is a visible
    // world pickup rather than an automatic reward.
    const float AttractionRadius = .42f;
    const float CollectionDuration = .32f;
    const float PlayerBodyYOffset = .15f;

    [Min(1)] public int amount = 1;

    SpriteRenderer visual;
    Transform collectionTarget;
    Vector3 collectionStart;
    Vector3 basePosition;
    Vector3 visualStartScale;
    float spawnedAt;
    float collectionStartedAt;
    Transform blockedPlayer;
    const float RetryDistancePadding = 1f;

    public static void Spawn(Vector2 position, int amount)
    {
        if (amount <= 0) return;

        var pickup = new GameObject("Dropped Wood");
        pickup.transform.position = position;
        var trigger = pickup.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = AttractionRadius;

        var visualObject = new GameObject("Visual");
        visualObject.transform.SetParent(pickup.transform, false);
        visualObject.transform.localScale = Vector3.one * WorldVisualScale;
        var renderer = visualObject.AddComponent<SpriteRenderer>();
        Texture2D texture = Resources.Load<Texture2D>("UI/wood-onground");
        if (texture)
        {
            texture.filterMode = FilterMode.Point;
            renderer.sprite = Sprite.Create(texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(.5f, .18f), PixelArtStandard.PixelsPerUnit);
        }
        renderer.sortingOrder = Mathf.RoundToInt(-position.y * 100f) + 1;

        var wood = pickup.AddComponent<WoodPickup>();
        wood.amount = amount;
        wood.basePosition = pickup.transform.position;
    }

    void Awake()
    {
        visual = GetComponentInChildren<SpriteRenderer>();
        spawnedAt = Time.time;
    }

    void Update()
    {
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

        if (!collectionTarget)
        {
            float bob = Mathf.Sin((Time.time - spawnedAt) * 4.5f) * .08f;
            transform.position = basePosition + Vector3.up * bob;
            return;
        }

        float progress = Mathf.Clamp01((Time.time - collectionStartedAt) / CollectionDuration);
        float eased = progress * progress * (3f - 2f * progress);
        transform.position = Vector3.Lerp(collectionStart,
            collectionTarget.position + Vector3.up * PlayerBodyYOffset, eased);
        if (visual) visual.transform.localScale = Vector3.Lerp(visualStartScale, visualStartScale * .2f, eased);
        if (progress < 1f) return;

        if (!ItemInventory.AddPickedUpItem(ItemInventory.Container.PlayerInventory, InventoryItemId.Wood, amount))
        {
            var fullHud = collectionTarget.GetComponent<ExpeditionHUD>()
                ? collectionTarget.GetComponent<ExpeditionHUD>()
                : FindAnyObjectByType<ExpeditionHUD>();
            if (fullHud) fullHud.ShowStatus("WOOD INVENTORY FULL");
            blockedPlayer = collectionTarget;
            collectionTarget = null;
            if (visual) visual.transform.localScale = visualStartScale;
            spawnedAt = Time.time;
            basePosition = collectionStart;
            transform.position = basePosition;
            return;
        }
        var hud = collectionTarget.GetComponent<ExpeditionHUD>()
            ? collectionTarget.GetComponent<ExpeditionHUD>()
            : FindAnyObjectByType<ExpeditionHUD>();
        if (hud)
        {
            hud.ShowStatus($"+{amount} WOOD");
        }
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other) => TryCollect(other);
    void OnTriggerStay2D(Collider2D other) => TryCollect(other);

    void TryCollect(Collider2D other)
    {
        if (collectionTarget || blockedPlayer) return;
        var player = other.GetComponentInParent<TownPlayerController>();
        if (!player) return;

        collectionTarget = player.transform;
        collectionStart = transform.position;
        visualStartScale = visual ? visual.transform.localScale : Vector3.one;
        collectionStartedAt = Time.time;
        var trigger = GetComponent<CircleCollider2D>();
        if (trigger) trigger.enabled = false;
    }
}
