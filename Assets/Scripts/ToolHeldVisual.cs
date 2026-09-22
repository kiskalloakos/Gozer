using UnityEngine;

/// <summary>Displays the equipped axe or sword in the player's right hand.</summary>
[RequireComponent(typeof(TownPlayerController))]
public sealed class ToolHeldVisual : MonoBehaviour
{
    const float WorldScale = .58f;
    static Texture2D axeTexture;
    static Texture2D swordTexture;

    TownPlayerController movement;
    SpriteRenderer heldVisual;
    Sprite axeSprite;
    Sprite swordSprite;

    void Awake()
    {
        movement = GetComponent<TownPlayerController>();
        var heldObject = new GameObject("Held Tool");
        heldObject.transform.SetParent(transform, false);
        heldVisual = heldObject.AddComponent<SpriteRenderer>();
        axeTexture = axeTexture ? axeTexture : Resources.Load<Texture2D>("UI/wooden_axe");
        swordTexture = swordTexture ? swordTexture : Resources.Load<Texture2D>("UI/wooden_sword");
        axeSprite = CreateToolSprite(axeTexture);
        swordSprite = CreateToolSprite(swordTexture);
    }

    void LateUpdate()
    {
        if (!heldVisual || !movement) return;

        var hud = FindAnyObjectByType<ExpeditionHUD>();
        InventoryItemId equippedItem = hud ? hud.ActiveQuickbarItem : InventoryItemId.Empty;
        Sprite equippedSprite = equippedItem == InventoryItemId.Sword ? swordSprite
            : equippedItem == InventoryItemId.Axe ? axeSprite : null;
        heldVisual.enabled = equippedSprite != null;
        if (!equippedSprite) return;

        heldVisual.sprite = equippedSprite;
        heldVisual.transform.localScale = Vector3.one * WorldScale;
        heldVisual.transform.localPosition = HandPosition(movement.CurrentFacing);
        heldVisual.transform.localRotation = Quaternion.Euler(0f, 0f, HandRotation(movement.CurrentFacing));
        heldVisual.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) + 2;
    }

    static Sprite CreateToolSprite(Texture2D texture)
    {
        if (!texture) return null;
        texture.filterMode = FilterMode.Point;
        return Sprite.Create(texture,
            new Rect(0f, 0f, texture.width, texture.height),
            // Anchor the tool at the grip rather than at the bottom of the
            // handle. This makes the hand position stable in all facings.
            new Vector2(.4f, .32f), PixelArtStandard.PixelsPerUnit);
    }

    static Vector3 HandPosition(TownPlayerController.FacingDirection facing)
    {
        switch (facing)
        {
            case TownPlayerController.FacingDirection.Right: return new Vector3(.52f, .88f, 0f);
            case TownPlayerController.FacingDirection.Up: return new Vector3(-.28f, .88f, 0f);
            case TownPlayerController.FacingDirection.Left: return new Vector3(-.52f, .88f, 0f);
            default: return new Vector3(.28f, .88f, 0f);
        }
    }

    static float HandRotation(TownPlayerController.FacingDirection facing)
    {
        switch (facing)
        {
            case TownPlayerController.FacingDirection.Right: return -35f;
            case TownPlayerController.FacingDirection.Up: return 155f;
            case TownPlayerController.FacingDirection.Left: return 35f;
            default: return 25f;
        }
    }

    void OnDestroy()
    {
        if (axeSprite) Destroy(axeSprite);
        if (swordSprite) Destroy(swordSprite);
    }
}
