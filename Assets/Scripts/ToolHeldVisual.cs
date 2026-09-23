using System;
using UnityEngine;

/// <summary>Displays the currently equipped tool with an editable visual profile per item.</summary>
[RequireComponent(typeof(TownPlayerController))]
[ExecuteAlways]
public sealed class ToolHeldVisual : MonoBehaviour
{
    public enum DrawDepth { BehindCharacter, InFrontOfCharacter }

    [Serializable]
    public sealed class ToolVisualProfile
    {
        public InventoryItemId item = InventoryItemId.Axe;

        [Header("Tool Art")]
        public Texture2D downTexture;
        public Texture2D rightTexture;
        public Texture2D upTexture;
        public Texture2D leftTexture;

        [Header("Pixel Scale")]
        public float worldScale = .58f;
        public Vector2 toolPivot = new Vector2(.4f, .32f);

        [Header("Resting Hand Position (X/Y)")]
        public Vector3 downHandPosition = new Vector3(.28f, .88f, 0f);
        public Vector3 rightHandPosition = new Vector3(.52f, .88f, 0f);
        public Vector3 upHandPosition = new Vector3(-.28f, .88f, 0f);
        public Vector3 leftHandPosition = new Vector3(-.52f, .88f, 0f);

        [Header("Resting Hand Rotation")]
        public float downHandRotation = 25f;
        public float rightHandRotation = -35f;
        public float upHandRotation = 155f;
        public float leftHandRotation = 35f;

        [Header("Walk Frame Position Offsets")]
        public Vector3[] downWalkFrameOffsets = new Vector3[4];
        public Vector3[] rightWalkFrameOffsets = new Vector3[4];
        public Vector3[] upWalkFrameOffsets = new Vector3[4];
        public Vector3[] leftWalkFrameOffsets = new Vector3[4];

        [Header("Attack Frame Position Offsets")]
        public Vector3[] downAttackFrameOffsets = new Vector3[3];
        public Vector3[] rightAttackFrameOffsets = new Vector3[3];
        public Vector3[] upAttackFrameOffsets = new Vector3[3];
        public Vector3[] leftAttackFrameOffsets = new Vector3[3];

        [Header("Attack Frame Rotation Offsets")]
        public float[] downAttackFrameRotationOffsets = new float[3];
        public float[] rightAttackFrameRotationOffsets = new float[3];
        public float[] upAttackFrameRotationOffsets = new float[3];
        public float[] leftAttackFrameRotationOffsets = new float[3];

        [Header("Draw Depth")]
        public DrawDepth downDrawDepth = DrawDepth.InFrontOfCharacter;
        public DrawDepth rightDrawDepth = DrawDepth.InFrontOfCharacter;
        public DrawDepth upDrawDepth = DrawDepth.BehindCharacter;
        public DrawDepth leftDrawDepth = DrawDepth.InFrontOfCharacter;
    }

    [Header("Tool Selector")]
    [SerializeField] InventoryItemId toolToConfigure = InventoryItemId.Axe;
    [SerializeField] ToolVisualProfile[] toolProfiles;

    [Header("Edit-mode Preview")]
    [SerializeField] bool showEditModePreview = true;
    [SerializeField] TownPlayerController.FacingDirection previewFacing =
        TownPlayerController.FacingDirection.Right;
    [SerializeField] bool previewWalking;
    [Range(0, 3)] [SerializeField] int previewWalkFrame;
    [SerializeField, HideInInspector] bool previewFacingInitialized;

    // Kept hidden so Unity can migrate the existing scene values into the Axe profile.
    [SerializeField, HideInInspector] Texture2D axeDownTexture;
    [SerializeField, HideInInspector] Texture2D axeRightTexture;
    [SerializeField, HideInInspector] Texture2D axeUpTexture;
    [SerializeField, HideInInspector] Texture2D axeLeftTexture;
    [SerializeField, HideInInspector] Texture2D swordTexture;
    [SerializeField, HideInInspector] float worldScale = .58f;
    [SerializeField, HideInInspector] Vector2 toolPivot = new Vector2(.4f, .32f);
    [SerializeField, HideInInspector] Vector3 downHandPosition = new Vector3(.28f, .88f, 0f);
    [SerializeField, HideInInspector] Vector3 rightHandPosition = new Vector3(.52f, .88f, 0f);
    [SerializeField, HideInInspector] Vector3 upHandPosition = new Vector3(-.28f, .88f, 0f);
    [SerializeField, HideInInspector] Vector3 leftHandPosition = new Vector3(-.52f, .88f, 0f);
    [SerializeField, HideInInspector] float downHandRotation = 25f;
    [SerializeField, HideInInspector] float rightHandRotation = -35f;
    [SerializeField, HideInInspector] float upHandRotation = 155f;
    [SerializeField, HideInInspector] float leftHandRotation = 35f;
    [SerializeField, HideInInspector] Vector3[] downWalkFrameOffsets = new Vector3[4];
    [SerializeField, HideInInspector] Vector3[] rightWalkFrameOffsets = new Vector3[4];
    [SerializeField, HideInInspector] Vector3[] upWalkFrameOffsets = new Vector3[4];
    [SerializeField, HideInInspector] Vector3[] leftWalkFrameOffsets = new Vector3[4];
    [SerializeField, HideInInspector] DrawDepth downDrawDepth = DrawDepth.InFrontOfCharacter;
    [SerializeField, HideInInspector] DrawDepth rightDrawDepth = DrawDepth.InFrontOfCharacter;
    [SerializeField, HideInInspector] DrawDepth upDrawDepth = DrawDepth.BehindCharacter;
    [SerializeField, HideInInspector] DrawDepth leftDrawDepth = DrawDepth.InFrontOfCharacter;

    TownPlayerController movement;
    SpriteRenderer heldVisual;
    Sprite downSprite;
    Sprite rightSprite;
    Sprite upSprite;
    Sprite leftSprite;
    InventoryItemId loadedProfileItem = InventoryItemId.Empty;
    bool spritesLoadedForCurrentItem;

    void Awake()
    {
        movement = GetComponent<TownPlayerController>();
        EnsureToolProfiles();
        EnsureProfileOffsetCounts();
        EnsureHeldVisual();
        LoadToolSprites(toolToConfigure, true);
    }

    void OnEnable()
    {
        movement = GetComponent<TownPlayerController>();
        EnsureToolProfiles();
        EnsureProfileOffsetCounts();
        EnsureHeldVisual();
        LoadToolSprites(toolToConfigure, true);
    }

    void OnValidate()
    {
        if (Application.isPlaying) return;

        if (!previewFacingInitialized)
        {
            previewFacing = TownPlayerController.FacingDirection.Right;
            previewFacingInitialized = true;
        }
        movement = GetComponent<TownPlayerController>();
        EnsureToolProfiles();
        EnsureProfileOffsetCounts();
        EnsureHeldVisual();
        LoadToolSprites(toolToConfigure, true);
    }

    /// <summary>Called by the editor setup command before copying profiles to other scenes.</summary>
    public void PrepareProfilesForEditor()
    {
        movement = GetComponent<TownPlayerController>();
        EnsureToolProfiles();
        EnsureProfileOffsetCounts();
    }

    void EnsureToolProfiles()
    {
        bool migrateLegacyAxe = toolProfiles == null || toolProfiles.Length == 0;
        if (migrateLegacyAxe)
        {
            toolProfiles = new ToolVisualProfile[ItemInventory.ToolItemIds.Count];
            for (int i = 0; i < ItemInventory.ToolItemIds.Count; i++)
                toolProfiles[i] = CreateDefaultProfile(ItemInventory.ToolItemIds[i]);

            CopyLegacyAxeSettings(FindProfile(InventoryItemId.Axe));
            if (swordTexture) SetAllDirections(FindProfile(InventoryItemId.Sword), swordTexture);
            return;
        }

        foreach (InventoryItemId item in ItemInventory.ToolItemIds)
        {
            if (FindProfile(item) != null) continue;
            var profile = CreateDefaultProfile(item);
            Array.Resize(ref toolProfiles, toolProfiles.Length + 1);
            toolProfiles[toolProfiles.Length - 1] = profile;
        }
    }

    ToolVisualProfile CreateDefaultProfile(InventoryItemId item)
    {
        var profile = new ToolVisualProfile { item = item };
        switch (item)
        {
            case InventoryItemId.Axe:
                profile.downTexture = Resources.Load<Texture2D>("UI/wooden_axe_front");
                profile.rightTexture = Resources.Load<Texture2D>("UI/wooden_axe_right");
                profile.upTexture = Resources.Load<Texture2D>("UI/wooden_axe_back");
                profile.leftTexture = Resources.Load<Texture2D>("UI/wooden_axe_left");
                break;
            case InventoryItemId.Pickaxe:
                SetAllDirections(profile, Resources.Load<Texture2D>("UI/wooden_pickaxe"));
                break;
            case InventoryItemId.Sword:
                SetAllDirections(profile, Resources.Load<Texture2D>("UI/wooden_sword"));
                break;
            case InventoryItemId.Shovel:
                SetAllDirections(profile, Resources.Load<Texture2D>("UI/wooden_shovel"));
                break;
            default:
                string resourceName = item.ToString().ToLowerInvariant();
                Texture2D texture = Resources.Load<Texture2D>("UI/wooden_" + resourceName);
                if (!texture) texture = Resources.Load<Texture2D>("UI/" + resourceName);
                SetAllDirections(profile, texture);
                break;
        }
        return profile;
    }

    static void SetAllDirections(ToolVisualProfile profile, Texture2D texture)
    {
        profile.downTexture = texture;
        profile.rightTexture = texture;
        profile.upTexture = texture;
        profile.leftTexture = texture;
    }

    void CopyLegacyAxeSettings(ToolVisualProfile profile)
    {
        if (profile == null) return;
        if (axeDownTexture) profile.downTexture = axeDownTexture;
        if (axeRightTexture) profile.rightTexture = axeRightTexture;
        if (axeUpTexture) profile.upTexture = axeUpTexture;
        if (axeLeftTexture) profile.leftTexture = axeLeftTexture;
        profile.worldScale = worldScale;
        profile.toolPivot = toolPivot;
        profile.downHandPosition = downHandPosition;
        profile.rightHandPosition = rightHandPosition;
        profile.upHandPosition = upHandPosition;
        profile.leftHandPosition = leftHandPosition;
        profile.downHandRotation = downHandRotation;
        profile.rightHandRotation = rightHandRotation;
        profile.upHandRotation = upHandRotation;
        profile.leftHandRotation = leftHandRotation;
        profile.downWalkFrameOffsets = CloneOffsets(downWalkFrameOffsets);
        profile.rightWalkFrameOffsets = CloneOffsets(rightWalkFrameOffsets);
        profile.upWalkFrameOffsets = CloneOffsets(upWalkFrameOffsets);
        profile.leftWalkFrameOffsets = CloneOffsets(leftWalkFrameOffsets);
        profile.downDrawDepth = downDrawDepth;
        profile.rightDrawDepth = rightDrawDepth;
        profile.upDrawDepth = upDrawDepth;
        profile.leftDrawDepth = leftDrawDepth;
    }

    static Vector3[] CloneOffsets(Vector3[] offsets)
        => offsets != null ? (Vector3[])offsets.Clone() : new Vector3[4];

    ToolVisualProfile FindProfile(InventoryItemId item)
    {
        if (toolProfiles == null) return null;
        foreach (var profile in toolProfiles)
            if (profile != null && profile.item == item) return profile;
        return null;
    }

    void EnsureProfileOffsetCounts()
    {
        if (toolProfiles == null) return;
        var combat = GetComponent<ExpeditionPlayerCombat>();
        foreach (var profile in toolProfiles)
        {
            if (profile == null) continue;
            if (movement != null)
            {
                ResizeOffsets(ref profile.downWalkFrameOffsets, movement.walkDown);
                ResizeOffsets(ref profile.rightWalkFrameOffsets, movement.walkRight);
                ResizeOffsets(ref profile.upWalkFrameOffsets, movement.walkUp);
                ResizeOffsets(ref profile.leftWalkFrameOffsets, movement.walkLeft);
            }
            int attackFrameCount = combat ? combat.GetCharacterAttackFrameCount(profile.item) : 0;
            if (attackFrameCount > 0)
            {
                ResizeOffsets(ref profile.downAttackFrameOffsets, attackFrameCount);
                ResizeOffsets(ref profile.rightAttackFrameOffsets, attackFrameCount);
                ResizeOffsets(ref profile.upAttackFrameOffsets, attackFrameCount);
                ResizeOffsets(ref profile.leftAttackFrameOffsets, attackFrameCount);
                ResizeOffsets(ref profile.downAttackFrameRotationOffsets, attackFrameCount);
                ResizeOffsets(ref profile.rightAttackFrameRotationOffsets, attackFrameCount);
                ResizeOffsets(ref profile.upAttackFrameRotationOffsets, attackFrameCount);
                ResizeOffsets(ref profile.leftAttackFrameRotationOffsets, attackFrameCount);
            }
        }
    }

    static void ResizeOffsets(ref Vector3[] offsets, int count)
    {
        if (count <= 0) return;
        if (offsets == null) offsets = new Vector3[count];
        else if (offsets.Length != count) Array.Resize(ref offsets, count);
    }

    static void ResizeOffsets(ref Vector3[] offsets, Sprite[] frames)
    {
        int frameCount = frames != null ? frames.Length : 0;
        if (frameCount == 0) return;
        if (offsets == null) offsets = new Vector3[frameCount];
        else if (offsets.Length != frameCount) Array.Resize(ref offsets, frameCount);
    }

    static void ResizeOffsets(ref float[] offsets, int count)
    {
        if (count <= 0) return;
        if (offsets == null) offsets = new float[count];
        else if (offsets.Length != count) Array.Resize(ref offsets, count);
    }

    void EnsureHeldVisual()
    {
        if (heldVisual) return;
        var heldObject = transform.Find("Held Tool");
        if (!heldObject)
        {
            var created = new GameObject("Held Tool");
            created.transform.SetParent(transform, false);
            heldObject = created.transform;
        }
        heldVisual = heldObject.GetComponent<SpriteRenderer>();
        if (!heldVisual) heldVisual = heldObject.gameObject.AddComponent<SpriteRenderer>();
    }

    void LoadToolSprites(InventoryItemId item, bool force = false)
    {
        if (!heldVisual) return;
        if (!force && loadedProfileItem == item && spritesLoadedForCurrentItem) return;

        DestroyToolSprites();
        loadedProfileItem = item;
        spritesLoadedForCurrentItem = true;
        var profile = FindProfile(item);
        if (profile == null) return;

        downSprite = CreateToolSprite(profile.downTexture, profile.toolPivot);
        rightSprite = CreateToolSprite(profile.rightTexture, profile.toolPivot);
        upSprite = CreateToolSprite(profile.upTexture, profile.toolPivot);
        leftSprite = CreateToolSprite(profile.leftTexture, profile.toolPivot);
    }

    void LateUpdate()
    {
        if (!heldVisual || !movement) return;

        if (!Application.isPlaying)
        {
            RefreshEditModePreview();
            return;
        }

        var hud = FindAnyObjectByType<ExpeditionHUD>();
        InventoryItemId equippedItem = hud ? hud.ActiveQuickbarItem : InventoryItemId.Empty;
        LoadToolSprites(equippedItem);
        var profileForItem = FindProfile(equippedItem);
        RefreshVisual(profileForItem, movement.CurrentFacing, false,
            SpriteForFacing(movement.CurrentFacing), movement.IsWalking,
            movement.CurrentAnimationFrameIndex);
    }

    public void RefreshEditModePreview()
    {
        if (Application.isPlaying) return;
        if (!movement) movement = GetComponent<TownPlayerController>();
        if (!heldVisual) EnsureHeldVisual();
        if (!movement || !heldVisual) return;
        EnsureToolProfiles();
        EnsureProfileOffsetCounts();
        var combat = GetComponent<ExpeditionPlayerCombat>();
        bool attackPreview = combat && combat.IsEditorAttackPreviewActive;
        InventoryItemId previewItem = attackPreview ? combat.EditorPreviewItem : toolToConfigure;
        LoadToolSprites(previewItem);
        TownPlayerController.FacingDirection facing = attackPreview ? combat.EditorPreviewFacing : previewFacing;
        bool walking = !attackPreview && previewWalking;
        if (!attackPreview)
            movement.SetEditorPreviewAnimation(facing, walking, previewWalkFrame);
        RefreshVisual(FindProfile(previewItem), facing, true, null,
            walking, previewWalkFrame, attackPreview ? combat.EditorPreviewCharacterFrame : -1);
    }

    void RefreshVisual(ToolVisualProfile profile, TownPlayerController.FacingDirection facing,
        bool preview, Sprite sprite, bool walking, int frameIndex, int attackFrameIndex = -1)
    {
        if (!heldVisual) return;
        heldVisual.enabled = profile != null && (preview ? showEditModePreview : sprite != null);
        if (!heldVisual.enabled) return;

        heldVisual.sprite = preview ? SpriteForFacing(facing) : sprite;
        heldVisual.transform.localScale = Vector3.one * profile.worldScale;
        Vector3 handPosition = HandPosition(profile, facing);
        float handRotation = HandRotation(profile, facing);
        if (attackFrameIndex >= 0)
        {
            handPosition += AttackFrameOffset(profile, facing, attackFrameIndex);
            handRotation += AttackFrameRotationOffset(profile, facing, attackFrameIndex);
        }
        else if (!preview && movement && movement.IsPlayingMeleeAttack)
        {
            // PlayMeleeAttack assigns frame 0 immediately, then advances the
            // public frame index to 1. Keep the held tool aligned to the sprite
            // currently on screen instead of applying frame 1 offsets to frame 0.
            handPosition += AttackFrameOffset(profile, facing, movement.CurrentMeleeAttackFrameIndex);
            handRotation += AttackFrameRotationOffset(profile, facing, movement.CurrentMeleeAttackFrameIndex);
        }
        else if (walking) handPosition += WalkFrameOffset(profile, facing, frameIndex);
        handPosition.z = 0f;
        heldVisual.transform.localPosition = handPosition;
        heldVisual.transform.localRotation = Quaternion.Euler(0f, 0f, handRotation);
        if (movement.visual) heldVisual.sortingLayerID = movement.visual.sortingLayerID;
        int characterOrder = movement.visual
            ? movement.visual.sortingOrder
            : Mathf.RoundToInt(-transform.position.y * 100f);
        heldVisual.sortingOrder = characterOrder +
            (ToolDrawDepth(profile, facing) == DrawDepth.BehindCharacter ? -1 : 1);
    }

    Sprite CreateToolSprite(Texture2D texture, Vector2 pivot)
    {
        if (!texture) return null;
        texture.filterMode = FilterMode.Point;
        return Sprite.Create(texture,
            new Rect(0f, 0f, texture.width, texture.height), pivot, PixelArtStandard.PixelsPerUnit);
    }

    Sprite SpriteForFacing(TownPlayerController.FacingDirection facing)
    {
        switch (facing)
        {
            case TownPlayerController.FacingDirection.Right: return rightSprite;
            case TownPlayerController.FacingDirection.Up: return upSprite;
            case TownPlayerController.FacingDirection.Left: return leftSprite;
            default: return downSprite;
        }
    }

    static Vector3 HandPosition(ToolVisualProfile profile, TownPlayerController.FacingDirection facing)
    {
        switch (facing)
        {
            case TownPlayerController.FacingDirection.Right: return profile.rightHandPosition;
            case TownPlayerController.FacingDirection.Up: return profile.upHandPosition;
            case TownPlayerController.FacingDirection.Left: return profile.leftHandPosition;
            default: return profile.downHandPosition;
        }
    }

    static float HandRotation(ToolVisualProfile profile, TownPlayerController.FacingDirection facing)
    {
        switch (facing)
        {
            case TownPlayerController.FacingDirection.Right: return profile.rightHandRotation;
            case TownPlayerController.FacingDirection.Up: return profile.upHandRotation;
            case TownPlayerController.FacingDirection.Left: return profile.leftHandRotation;
            default: return profile.downHandRotation;
        }
    }

    static Vector3 WalkFrameOffset(ToolVisualProfile profile,
        TownPlayerController.FacingDirection facing, int frameIndex)
    {
        Vector3[] offsets;
        switch (facing)
        {
            case TownPlayerController.FacingDirection.Right: offsets = profile.rightWalkFrameOffsets; break;
            case TownPlayerController.FacingDirection.Up: offsets = profile.upWalkFrameOffsets; break;
            case TownPlayerController.FacingDirection.Left: offsets = profile.leftWalkFrameOffsets; break;
            default: offsets = profile.downWalkFrameOffsets; break;
        }
        return offsets != null && offsets.Length > 0
            ? offsets[Mathf.Clamp(frameIndex, 0, offsets.Length - 1)]
            : Vector3.zero;
    }

    static Vector3 AttackFrameOffset(ToolVisualProfile profile,
        TownPlayerController.FacingDirection facing, int frameIndex)
    {
        Vector3[] offsets;
        switch (facing)
        {
            case TownPlayerController.FacingDirection.Right: offsets = profile.rightAttackFrameOffsets; break;
            case TownPlayerController.FacingDirection.Up: offsets = profile.upAttackFrameOffsets; break;
            case TownPlayerController.FacingDirection.Left: offsets = profile.leftAttackFrameOffsets; break;
            default: offsets = profile.downAttackFrameOffsets; break;
        }
        return offsets != null && offsets.Length > 0
            ? offsets[Mathf.Clamp(frameIndex, 0, offsets.Length - 1)]
            : Vector3.zero;
    }

    static float AttackFrameRotationOffset(ToolVisualProfile profile,
        TownPlayerController.FacingDirection facing, int frameIndex)
    {
        float[] offsets;
        switch (facing)
        {
            case TownPlayerController.FacingDirection.Right: offsets = profile.rightAttackFrameRotationOffsets; break;
            case TownPlayerController.FacingDirection.Up: offsets = profile.upAttackFrameRotationOffsets; break;
            case TownPlayerController.FacingDirection.Left: offsets = profile.leftAttackFrameRotationOffsets; break;
            default: offsets = profile.downAttackFrameRotationOffsets; break;
        }
        return offsets != null && offsets.Length > 0
            ? offsets[Mathf.Clamp(frameIndex, 0, offsets.Length - 1)]
            : 0f;
    }

    static DrawDepth ToolDrawDepth(ToolVisualProfile profile, TownPlayerController.FacingDirection facing)
    {
        switch (facing)
        {
            case TownPlayerController.FacingDirection.Right: return profile.rightDrawDepth;
            case TownPlayerController.FacingDirection.Up: return profile.upDrawDepth;
            case TownPlayerController.FacingDirection.Left: return profile.leftDrawDepth;
            default: return profile.downDrawDepth;
        }
    }

    void OnDestroy() => DestroyToolSprites();

    void DestroyToolSprites()
    {
        DestroySprite(downSprite);
        DestroySprite(rightSprite);
        DestroySprite(upSprite);
        DestroySprite(leftSprite);
        downSprite = rightSprite = upSprite = leftSprite = null;
        spritesLoadedForCurrentItem = false;
    }

    static void DestroySprite(Sprite sprite)
    {
        if (!sprite) return;
        if (Application.isPlaying) Destroy(sprite);
        else DestroyImmediate(sprite);
    }
}
