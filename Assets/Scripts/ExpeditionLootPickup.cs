using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
public class ExpeditionLootPickup : MonoBehaviour
{
    [Min(1)] public int amount = 1;

    static Sprite placeholderSprite;

    public static void Spawn(Vector2 position, int amount)
    {
        if (amount <= 0) return;

        var pickup = new GameObject("Dropped Expedition Supplies");
        pickup.transform.position = position;

        var renderer = pickup.AddComponent<SpriteRenderer>();
        renderer.sprite = GetPlaceholderSprite();
        renderer.color = new Color(1f, .68f, .16f);
        renderer.sortingOrder = Mathf.RoundToInt(-position.y * 100) + 1;

        var trigger = pickup.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = .42f;

        var loot = pickup.AddComponent<ExpeditionLootPickup>();
        loot.amount = amount;
    }

    void Update()
    {
        transform.Rotate(0f, 0f, 55f * Time.deltaTime);
        float pulse = 1f + Mathf.Sin(Time.time * 5f) * .1f;
        transform.localScale = Vector3.one * pulse;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var inventory = other.GetComponentInParent<ExpeditionHUD>();
        if (!inventory) return;

        inventory.AddLoot(amount);
        Destroy(gameObject);
    }

    static Sprite GetPlaceholderSprite()
    {
        if (placeholderSprite) return placeholderSprite;

        var texture = new Texture2D(5, 5, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "Expedition Loot Placeholder"
        };
        var clear = new Color(0f, 0f, 0f, 0f);
        var gold = Color.white;
        var pixels = new Color[25];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
        pixels[2] = gold;
        pixels[6] = gold;
        pixels[7] = gold;
        pixels[8] = gold;
        for (int x = 0; x < 5; x++) pixels[10 + x] = gold;
        pixels[16] = gold;
        pixels[17] = gold;
        pixels[18] = gold;
        pixels[22] = gold;
        texture.SetPixels(pixels);
        texture.Apply();

        placeholderSprite = Sprite.Create(texture, new Rect(0, 0, 5, 5), new Vector2(.5f, .5f), 5f);
        placeholderSprite.name = "Expedition Loot Placeholder";
        return placeholderSprite;
    }
}
