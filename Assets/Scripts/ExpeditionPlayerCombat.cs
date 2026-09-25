using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(1000)]
public class ExpeditionPlayerCombat : MonoBehaviour
{
    // Zero-based animation index: 2 means the third displayed attack frame.
    // Change this one value to move all melee impacts to another frame.
    const int DamageFrameIndex = 2;

    [Serializable]
    public sealed class ToolCombatProfile
    {
        public InventoryItemId item;
        [Header("Attack")]
        [Min(.1f)] public float attackRange = .9f;
        [Min(.1f)] public float attackRadius = .7f;
        [Min(.05f)] public float attackCooldown = .38f;
        [Min(0)] public int damageBonus;
        [Min(0f)] public float knockbackDistance = 1.15f;
        [Min(0f)] public float energyCost = 1f;
        [Header("Character use animation (4 rows: down, right, up, left)")]
        public Texture2D characterAttackSheet;
        [Tooltip("Optional custom-cut attack sprites in animation order, populated from the combat sheet's Sprite Editor slices.")]
        public Sprite[] downCharacterAttackSprites;
        public Sprite[] rightCharacterAttackSprites;
        public Sprite[] upCharacterAttackSprites;
        public Sprite[] leftCharacterAttackSprites;
        [Min(1)] public int characterFrameWidth = 16;
        [Min(1)] public int characterFrameHeight = 32;
        [Min(1)] public int characterFrameCount = 3;
        [Min(.01f)] public float characterFramesPerSecond = 12f;
        [Tooltip("Logical attack frame to source-sheet column mapping. Frame 0 is the ready/idle pose; adjust per facing if a row is laid out differently.")]
        public int[] downAttackFrameOrder = { 0, 1, 2 };
        public int[] rightAttackFrameOrder = { 0, 1, 2 };
        public int[] upAttackFrameOrder = { 0, 1, 2 };
        public int[] leftAttackFrameOrder = { 0, 1, 2 };
    [Header("Hit effect")]
        public bool showSwoosh = true;
        public Texture2D swooshSheet;
        [Min(.01f)] public float swooshFrameDuration = .055f;
        [Min(.1f)] public float swooshScale = .25f;
        [Min(0f)] public float swooshHeightAboveFeet = 1f;
        [Min(1)] public int swooshFrameHeight = 32;
    }

    public event Action Attacked;

    [Min(.1f)] public float attackRange = .9f;
    [Min(.1f)] public float attackRadius = .7f;
    [Min(.05f)] public float attackCooldown = .38f;
    [Min(1)] public int damage = 1;
    [Min(1)] public int swordDamageBonus = 1;
    [Min(0f)] public float knockbackDistance = 1.15f;
    [Header("Energy")]
    [Min(0f)] public float attackEnergyCost = 1f;
    [Header("Attack effect")]
    public Texture2D swooshSheet;
    [Min(.01f)] public float swooshFrameDuration = .055f;
    [Min(.1f)] public float swooshScale = .25f;
    [Min(0f)] public float swooshHeightAboveFeet = 1f;
    [Min(1)] public int swooshFrameHeight = 32;
    [Header("Per-tool combat setup")]
    public ToolCombatProfile[] toolProfiles;
    [Header("Unarmed combat")]
    [SerializeField] ToolCombatProfile unarmedProfile;
    [Header("Edit-mode attack preview")]
    [SerializeField] bool showAttackPreview = true;
    [SerializeField] TownPlayerController.FacingDirection previewAttackFacing;
    [SerializeField, Min(0)] int previewCharacterFrame;
    [SerializeField] bool previewHitEffect = true;
    [SerializeField, Min(0)] int previewHitEffectFrame;
    [SerializeField, HideInInspector] int previewToolIndex;

    float nextAttackTime;
    ExpeditionPlayerHealth health;
    ExpeditionPlayerEnergy energy;
    TownPlayerController movementController;
    GameObject activeSwoosh;
    GameObject previewSwooshObject;
    SpriteRenderer previewSwooshRenderer;
    ToolCombatProfile pendingAttackProfile;
    Vector2 pendingAttackDirection;
    InventoryItemId pendingAttackTool;
    int pendingAttackDamage;
    bool pendingAttack;
    Sprite previewCharacterSprite;
    Sprite previewSwooshSprite;
    Texture2D previewCharacterSheet;
    Texture2D previewSwooshSheet;
    int previewCharacterFrameCache = -1;
    int previewSwooshFrameCache = -1;
    int previewCharacterWidthCache;
    int previewCharacterHeightCache;
    int previewCharacterFacingCache = -1;
    int previewCharacterSourceFrameCache = -1;
    int previewEffectFrameHeightCache;
    bool wasShowingAttackPreview;
    public bool IsEditorAttackPreviewActive => !Application.isPlaying && showAttackPreview;
    public TownPlayerController.FacingDirection EditorPreviewFacing => previewAttackFacing;
    public int EditorPreviewCharacterFrame => previewCharacterFrame;
    public InventoryItemId EditorPreviewItem
        => ItemInventory.ToolItemIds.Count > 0
            ? ItemInventory.ToolItemIds[Mathf.Clamp(previewToolIndex, 0, ItemInventory.ToolItemIds.Count - 1)]
            : InventoryItemId.Empty;

    void Awake()
    {
        health = GetComponent<ExpeditionPlayerHealth>();
        movementController = GetComponent<TownPlayerController>();
        EnsureToolProfiles();
        if (!Application.isPlaying)
            return;
        energy = GetComponent<ExpeditionPlayerEnergy>();
        if (!energy) energy = gameObject.AddComponent<ExpeditionPlayerEnergy>();
        // Keep existing expedition scenes working without requiring a manual
        // inspector assignment after this effect is added.
        if (!swooshSheet) swooshSheet = Resources.Load<Texture2D>("Effects/melee_swoosh");
        if (swooshSheet) swooshSheet.filterMode = FilterMode.Point;
    }

    void OnValidate()
    {
        EnsureToolProfiles();
    }

    void LateUpdate()
    {
        if (Application.isPlaying) return;
        if (showAttackPreview)
        {
            wasShowingAttackPreview = true;
            RefreshEditorPreview();
            return;
        }

        if (wasShowingAttackPreview)
        {
            DestroyEditorPreview();
            wasShowingAttackPreview = false;
        }
        var heldVisual = GetComponent<ToolHeldVisual>();
        if (heldVisual) heldVisual.RefreshEditModePreview();
    }

    public void RefreshEditorPreview()
    {
        if (Application.isPlaying) return;
        if (!showAttackPreview)
        {
            DestroyEditorPreview();
            wasShowingAttackPreview = false;
            var heldVisual = GetComponent<ToolHeldVisual>();
            if (heldVisual) heldVisual.RefreshEditModePreview();
            return;
        }
        wasShowingAttackPreview = true;

        if (!movementController) movementController = GetComponent<TownPlayerController>();
        if (!movementController) return;
        var tools = ItemInventory.ToolItemIds;
        if (tools.Count == 0) return;
        previewToolIndex = Mathf.Clamp(previewToolIndex, 0, tools.Count - 1);
        ToolCombatProfile profile = FindProfile(tools[previewToolIndex]);
        if (profile == null) return;

        movementController.SetEditorPreviewAnimation(previewAttackFacing, false, 0);
        Sprite[] authoredFrames = CharacterAttackFramesFor(profile, previewAttackFacing);
        if (authoredFrames != null && authoredFrames.Length > 0)
        {
            int frame = Mathf.Clamp(previewCharacterFrame, 0, authoredFrames.Length - 1);
            if (movementController.visual) movementController.visual.sprite = authoredFrames[frame];
        }
        else if (profile.characterAttackSheet && profile.characterFrameWidth > 0
            && profile.characterFrameHeight > 0 && profile.characterFrameCount > 0)
        {
            int rows = profile.characterAttackSheet.height / profile.characterFrameHeight;
            int columns = profile.characterAttackSheet.width / profile.characterFrameWidth;
            int rowFromTop = (int)previewAttackFacing;
            if (columns > 0 && rowFromTop < rows)
            {
                int frame = Mathf.Clamp(previewCharacterFrame, 0,
                    Mathf.Min(columns, profile.characterFrameCount) - 1);
                int sourceFrame = GetSourceAttackFrame(profile, previewAttackFacing, frame,
                    Mathf.Min(columns, profile.characterFrameCount));
                if (previewCharacterSheet != profile.characterAttackSheet
                    || previewCharacterFrameCache != frame
                    || previewCharacterSourceFrameCache != sourceFrame
                    || previewCharacterWidthCache != profile.characterFrameWidth
                    || previewCharacterHeightCache != profile.characterFrameHeight
                    || previewCharacterFacingCache != rowFromTop
                    || !previewCharacterSprite)
                {
                    DestroyPreviewSprite(ref previewCharacterSprite);
                    int y = (rows - rowFromTop - 1) * profile.characterFrameHeight;
                    previewCharacterSprite = Sprite.Create(profile.characterAttackSheet,
                        new Rect(sourceFrame * profile.characterFrameWidth, y,
                            profile.characterFrameWidth, profile.characterFrameHeight),
                        new Vector2(.5f, 0f), PixelArtStandard.PixelsPerUnit);
                    previewCharacterSheet = profile.characterAttackSheet;
                    previewCharacterFrameCache = frame;
                    previewCharacterSourceFrameCache = sourceFrame;
                    previewCharacterWidthCache = profile.characterFrameWidth;
                    previewCharacterHeightCache = profile.characterFrameHeight;
                    previewCharacterFacingCache = rowFromTop;
                }
                if (movementController.visual) movementController.visual.sprite = previewCharacterSprite;
            }
        }

        var heldToolVisual = GetComponent<ToolHeldVisual>();
        if (heldToolVisual) heldToolVisual.RefreshEditModePreview();

        if (!previewHitEffect || !profile.showSwoosh || !profile.swooshSheet || profile.swooshFrameHeight < 1)
        {
            DestroyPreviewSwoosh();
            return;
        }

        int effectFrameCount = profile.swooshSheet.height / profile.swooshFrameHeight;
        if (effectFrameCount < 1) { DestroyPreviewSwoosh(); return; }
        int effectFrame = Mathf.Clamp(previewHitEffectFrame, 0, effectFrameCount - 1);
        if (previewSwooshSheet != profile.swooshSheet || previewSwooshFrameCache != effectFrame
            || previewEffectFrameHeightCache != profile.swooshFrameHeight || !previewSwooshSprite)
        {
            DestroyPreviewSprite(ref previewSwooshSprite);
            int y = (effectFrameCount - effectFrame - 1) * profile.swooshFrameHeight;
            previewSwooshSprite = Sprite.Create(profile.swooshSheet,
                new Rect(0f, y, profile.swooshSheet.width, profile.swooshFrameHeight),
                new Vector2(0f, .5f), PixelArtStandard.PixelsPerUnit);
            previewSwooshSheet = profile.swooshSheet;
            previewSwooshFrameCache = effectFrame;
            previewEffectFrameHeightCache = profile.swooshFrameHeight;
        }

        EnsurePreviewSwoosh();
        Vector2 direction = PreviewDirection(previewAttackFacing);
        previewSwooshObject.transform.position = transform.position
            + Vector3.up * profile.swooshHeightAboveFeet + (Vector3)(direction * .1f);
        previewSwooshObject.transform.rotation = Quaternion.FromToRotation(Vector3.right, direction);
        previewSwooshObject.transform.localScale = Vector3.one * profile.swooshScale;
        previewSwooshRenderer.sprite = previewSwooshSprite;
        previewSwooshRenderer.sortingOrder = movementController.visual
            ? movementController.visual.sortingOrder + 10
            : Mathf.RoundToInt(-transform.position.y * 100f) + 10;
    }

    static Vector2 PreviewDirection(TownPlayerController.FacingDirection facing)
    {
        switch (facing)
        {
            case TownPlayerController.FacingDirection.Right: return Vector2.right;
            case TownPlayerController.FacingDirection.Up: return Vector2.up;
            case TownPlayerController.FacingDirection.Left: return Vector2.left;
            default: return Vector2.down;
        }
    }

    void EnsurePreviewSwoosh()
    {
        if (previewSwooshObject) return;
        previewSwooshObject = new GameObject("Combat Preview Swoosh") { hideFlags = HideFlags.HideAndDontSave };
        previewSwooshRenderer = previewSwooshObject.AddComponent<SpriteRenderer>();
    }

    void DestroyEditorPreview()
    {
        if (movementController && movementController.visual)
            movementController.SetEditorPreviewAnimation(movementController.CurrentFacing, false, 0);
        DestroyPreviewSwoosh();
        DestroyPreviewSprite(ref previewCharacterSprite);
        DestroyPreviewSprite(ref previewSwooshSprite);
        previewCharacterSheet = previewSwooshSheet = null;
        previewCharacterFrameCache = previewSwooshFrameCache = -1;
        previewCharacterFacingCache = -1;
    }

    void DestroyPreviewSwoosh()
    {
        if (previewSwooshObject) DestroyImmediate(previewSwooshObject);
        previewSwooshObject = null;
        previewSwooshRenderer = null;
    }

    static void DestroyPreviewSprite(ref Sprite sprite)
    {
        if (sprite) DestroyImmediate(sprite);
        sprite = null;
    }

    void OnDisable()
    {
        if (!Application.isPlaying) DestroyEditorPreview();
    }

    void OnDestroy()
    {
        if (!Application.isPlaying) DestroyEditorPreview();
    }

    public void PrepareProfilesForEditor() => EnsureToolProfiles();

    void EnsureToolProfiles()
    {
        if (!movementController) movementController = GetComponent<TownPlayerController>();
        var tools = ItemInventory.ToolItemIds;
        if (toolProfiles == null) toolProfiles = Array.Empty<ToolCombatProfile>();
        foreach (var existing in toolProfiles)
            if (existing != null)
            {
                if (existing.downAttackFrameOrder == null) existing.downAttackFrameOrder = new[] { 0, 1, 2 };
                if (existing.rightAttackFrameOrder == null) existing.rightAttackFrameOrder = new[] { 0, 1, 2 };
                if (existing.upAttackFrameOrder == null) existing.upAttackFrameOrder = new[] { 0, 1, 2 };
                if (existing.leftAttackFrameOrder == null) existing.leftAttackFrameOrder = new[] { 0, 1, 2 };
            }
        foreach (InventoryItemId item in tools)
        {
            if (FindProfile(item) != null) continue;
            var profile = new ToolCombatProfile { item = item };
            profile.characterAttackSheet = movementController ? movementController.meleeAttackSheet : null;
            profile.attackRange = attackRange;
            profile.attackRadius = attackRadius;
            profile.attackCooldown = attackCooldown;
            profile.knockbackDistance = knockbackDistance;
            profile.energyCost = attackEnergyCost;
            profile.swooshSheet = swooshSheet ? swooshSheet : Resources.Load<Texture2D>("Effects/melee_swoosh");
            profile.swooshFrameDuration = swooshFrameDuration;
            profile.swooshScale = swooshScale;
            profile.swooshHeightAboveFeet = swooshHeightAboveFeet;
            profile.swooshFrameHeight = swooshFrameHeight;
            if (item == InventoryItemId.Axe || item == InventoryItemId.Pickaxe || item == InventoryItemId.Shovel)
            {
                profile.damageBonus = 0;
            }
            else if (item == InventoryItemId.Sword)
            {
                profile.damageBonus = swordDamageBonus;
            }
            var list = new List<ToolCombatProfile>(toolProfiles) { profile };
            toolProfiles = list.ToArray();
        }

        if (unarmedProfile == null)
        {
            unarmedProfile = new ToolCombatProfile
            {
                item = InventoryItemId.Empty,
                attackRange = .9f,
                attackRadius = .7f,
                attackCooldown = .38f,
                damageBonus = 0,
                knockbackDistance = 1.15f,
                energyCost = .25f,
                showSwoosh = false
            };
        }

        ToolCombatProfile animationTemplate = toolProfiles != null && toolProfiles.Length > 0
            ? toolProfiles[0]
            : null;
        Texture2D combatSheet = movementController && movementController.meleeAttackSheet
            ? movementController.meleeAttackSheet
            : animationTemplate != null ? animationTemplate.characterAttackSheet : null;
        unarmedProfile.item = InventoryItemId.Empty;
        unarmedProfile.showSwoosh = false;
        if (combatSheet) unarmedProfile.characterAttackSheet = combatSheet;
        if (animationTemplate != null)
        {
            unarmedProfile.downCharacterAttackSprites = animationTemplate.downCharacterAttackSprites;
            unarmedProfile.rightCharacterAttackSprites = animationTemplate.rightCharacterAttackSprites;
            unarmedProfile.upCharacterAttackSprites = animationTemplate.upCharacterAttackSprites;
            unarmedProfile.leftCharacterAttackSprites = animationTemplate.leftCharacterAttackSprites;
            unarmedProfile.characterFrameWidth = animationTemplate.characterFrameWidth;
            unarmedProfile.characterFrameHeight = animationTemplate.characterFrameHeight;
            unarmedProfile.characterFrameCount = animationTemplate.characterFrameCount;
            unarmedProfile.characterFramesPerSecond = animationTemplate.characterFramesPerSecond;
            unarmedProfile.downAttackFrameOrder = animationTemplate.downAttackFrameOrder;
            unarmedProfile.rightAttackFrameOrder = animationTemplate.rightAttackFrameOrder;
            unarmedProfile.upAttackFrameOrder = animationTemplate.upAttackFrameOrder;
            unarmedProfile.leftAttackFrameOrder = animationTemplate.leftAttackFrameOrder;
        }
    }

    static int GetSourceAttackFrame(ToolCombatProfile profile,
        TownPlayerController.FacingDirection facing, int logicalFrame, int frameCount)
    {
        int[] order = facing switch
        {
            TownPlayerController.FacingDirection.Right => profile.rightAttackFrameOrder,
            TownPlayerController.FacingDirection.Up => profile.upAttackFrameOrder,
            TownPlayerController.FacingDirection.Left => profile.leftAttackFrameOrder,
            _ => profile.downAttackFrameOrder
        };
        if (order == null || logicalFrame < 0 || logicalFrame >= order.Length)
            return Mathf.Clamp(logicalFrame, 0, frameCount - 1);
        return Mathf.Clamp(order[logicalFrame], 0, frameCount - 1);
    }

    static Sprite[] CharacterAttackFramesFor(ToolCombatProfile profile,
        TownPlayerController.FacingDirection facing)
    {
        if (profile == null) return null;
        return facing switch
        {
            TownPlayerController.FacingDirection.Right => profile.rightCharacterAttackSprites,
            TownPlayerController.FacingDirection.Up => profile.upCharacterAttackSprites,
            TownPlayerController.FacingDirection.Left => profile.leftCharacterAttackSprites,
            _ => profile.downCharacterAttackSprites
        };
    }

    ToolCombatProfile FindProfile(InventoryItemId item)
    {
        if (toolProfiles != null)
            foreach (var profile in toolProfiles)
                if (profile != null && profile.item == item) return profile;
        return null;
    }

    public int GetCharacterAttackFrameCount(InventoryItemId item)
    {
        ToolCombatProfile profile = FindProfile(item);
        if (profile == null) return 0;
        Sprite[][] authoredFrames = new[]
        {
            profile.downCharacterAttackSprites, profile.rightCharacterAttackSprites,
            profile.upCharacterAttackSprites, profile.leftCharacterAttackSprites
        };
        int authoredCount = 0;
        foreach (var frames in authoredFrames)
            if (frames != null) authoredCount = Mathf.Max(authoredCount, frames.Length);
        if (authoredCount > 0) return authoredCount;
        int count = Mathf.Max(1, profile.characterFrameCount);
        if (profile.characterAttackSheet && profile.characterFrameWidth > 0)
            count = Mathf.Min(count, profile.characterAttackSheet.width / profile.characterFrameWidth);
        return Mathf.Max(1, count);
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        if (GameSessionFlow.IsBlockingGameplay) return;
        if (PlayerInventoryUI.IsOpen || HomeStorageChest.IsModalOpen
            || WorkbenchCraftingUI.IsModalOpen) return;
        if (health && health.IsDefeated) return;
        if (!Input.GetMouseButtonDown(0)) return;
        var hud = FindAnyObjectByType<ExpeditionHUD>();
        InventoryItemId equippedItem = hud ? hud.ActiveQuickbarItem : InventoryItemId.Empty;
        ToolCombatProfile combatProfile = IsWoodenTool(equippedItem)
            ? FindProfile(equippedItem)
            : unarmedProfile;
        if (combatProfile == null || Time.time < nextAttackTime) return;

        var camera = Camera.main;
        if (camera && IsTownInteractionUnderCursor(camera)) return;
        Vector2 facing = movementController ? movementController.FacingVector : Vector2.down;
        Vector2 direction = camera
            ? (Vector2)camera.ScreenToWorldPoint(Input.mousePosition) - (Vector2)transform.position
            : facing;
        if (direction.sqrMagnitude < .01f) direction = facing;
        direction.Normalize();

        // Aim freely inside the forward-facing half of the player, but do not
        // start an attack when the cursor is behind the character.
        if (Vector2.Dot(direction, facing) < 0f) return;

        // Resolve the swing against the live scene when its impact frame starts.
        // Misses therefore do not consume energy.
        nextAttackTime = Time.time + combatProfile.attackCooldown;
        Attacked?.Invoke();

        pendingAttackProfile = combatProfile;
        pendingAttackDirection = direction;
        pendingAttackTool = equippedItem;
        pendingAttackDamage = damage + combatProfile.damageBonus;
        pendingAttack = true;

        if (movementController)
        {
            bool hasCustomSprites = combatProfile.downCharacterAttackSprites?.Length > 0
                || combatProfile.rightCharacterAttackSprites?.Length > 0
                || combatProfile.upCharacterAttackSprites?.Length > 0
                || combatProfile.leftCharacterAttackSprites?.Length > 0;
            if (hasCustomSprites)
                movementController.PlayMeleeAttack(direction,
                    combatProfile.downCharacterAttackSprites, combatProfile.rightCharacterAttackSprites,
                    combatProfile.upCharacterAttackSprites, combatProfile.leftCharacterAttackSprites,
                    combatProfile.characterFramesPerSecond, OnMeleeAttackFrameStarted);
            else
                movementController.PlayMeleeAttack(direction,
                    combatProfile.characterAttackSheet, combatProfile.characterFrameWidth,
                    combatProfile.characterFrameHeight, combatProfile.characterFrameCount,
                    combatProfile.characterFramesPerSecond, combatProfile.downAttackFrameOrder,
                    combatProfile.rightAttackFrameOrder, combatProfile.upAttackFrameOrder,
                    combatProfile.leftAttackFrameOrder, OnMeleeAttackFrameStarted);
        }
        else if (pendingAttack)
            ResolvePendingAttackHit();
        if (combatProfile.showSwoosh)
        {
            Vector2 visualOrigin = (Vector2)transform.position + Vector2.up * combatProfile.swooshHeightAboveFeet;
            ShowSwoosh(visualOrigin + direction * .1f, direction, combatProfile);
        }
    }

    static bool IsWoodenTool(InventoryItemId item)
        => item == InventoryItemId.Axe || item == InventoryItemId.Pickaxe
            || item == InventoryItemId.Shovel || item == InventoryItemId.Sword;

    void OnMeleeAttackFrameStarted(int frameIndex)
    {
        if (pendingAttack && frameIndex >= DamageFrameIndex)
            ResolvePendingAttackHit();
    }

    void ResolvePendingAttackHit()
    {
        if (!pendingAttack) return;

        pendingAttack = false;
        ToolCombatProfile profile = pendingAttackProfile;
        Vector2 direction = pendingAttackDirection;
        InventoryItemId equippedItem = pendingAttackTool;
        int attackDamage = pendingAttackDamage;
        pendingAttackProfile = null;

        if (profile == null) return;
        FindAttackTargets(direction, profile, out ChoppableTree tree,
            out WildernessEnemy target, out WildernessEnemy secondTarget);

        // Trees are resource nodes; only the equipped axe can chop them.
        if (tree && equippedItem != InventoryItemId.Axe) return;
        if (!tree && !target) return;

        if (energy && !energy.TryConsume(profile.energyCost))
        {
            FindAnyObjectByType<ExpeditionHUD>()?.ShowStatus("NOT ENOUGH ENERGY");
            return;
        }

        bool hitLanded;
        if (tree)
        {
            hitLanded = tree.TryChop(equippedItem);
        }
        else
        {
            hitLanded = target.TakeDamage(attackDamage, transform.position, profile.knockbackDistance);
            if (secondTarget && equippedItem == InventoryItemId.Sword)
                hitLanded |= secondTarget.TakeDamage(attackDamage, transform.position,
                    profile.knockbackDistance);
        }

        // A target can become unavailable during the wind-up. Refund the
        // impact-time charge if its hit handler reports that nothing landed.
        if (!hitLanded && energy) energy.Restore(profile.energyCost);
    }

    void FindAttackTargets(Vector2 direction, ToolCombatProfile profile,
        out ChoppableTree treeTarget, out WildernessEnemy target, out WildernessEnemy secondTarget)
    {
        treeTarget = null;
        target = null;
        secondTarget = null;
        float nearestTree = float.PositiveInfinity;
        float nearestObstacle = float.PositiveInfinity;
        var enemyDistances = new Dictionary<WildernessEnemy, float>();
        foreach (var hit in Physics2D.CircleCastAll(transform.position, profile.attackRadius * .5f,
                     direction, profile.attackRange + profile.attackRadius * .5f))
        {
            if (!hit.collider || hit.collider.transform == transform
                || hit.collider.transform.IsChildOf(transform)) continue;
            var tree = hit.collider.transform.GetComponentInParent<ChoppableTree>();
            var enemy = hit.collider.transform.GetComponentInParent<WildernessEnemy>();
            if (!tree && !enemy)
            {
                if (!hit.collider.isTrigger) nearestObstacle = Mathf.Min(nearestObstacle, hit.distance);
                continue;
            }

            Vector2 targetPosition = tree ? (Vector2)tree.transform.position : (Vector2)enemy.transform.position;
            if (Vector2.Dot(targetPosition - (Vector2)transform.position, direction) <= 0f
                || hit.distance >= nearestObstacle) continue;
            if (tree)
            {
                if (hit.distance < nearestTree)
                {
                    treeTarget = tree;
                    nearestTree = hit.distance;
                }
            }
            else if (!enemyDistances.TryGetValue(enemy, out float previousDistance)
                     || hit.distance < previousDistance)
                enemyDistances[enemy] = hit.distance;
        }

        // A solid collider before a target blocks the swing.
        if (nearestObstacle < nearestTree) treeTarget = null;
        float nearestEnemy = float.PositiveInfinity;
        float secondNearestEnemy = float.PositiveInfinity;
        foreach (var pair in enemyDistances)
        {
            if (pair.Value >= nearestObstacle) continue;
            if (pair.Value < nearestEnemy)
            {
                secondTarget = target;
                secondNearestEnemy = nearestEnemy;
                target = pair.Key;
                nearestEnemy = pair.Value;
            }
            else if (pair.Value < secondNearestEnemy)
            {
                secondTarget = pair.Key;
                secondNearestEnemy = pair.Value;
            }
        }

        if (treeTarget && nearestTree <= nearestEnemy)
        {
            target = null;
            secondTarget = null;
        }
        else
        {
            treeTarget = null;
        }
    }

    static bool IsTownInteractionUnderCursor(Camera camera)
    {
        Vector2 pointer = camera.ScreenToWorldPoint(Input.mousePosition);
        Collider2D collider = Physics2D.OverlapPoint(pointer);
        if (!collider) return false;
        return collider.GetComponentInParent<TownInteractable>()
            || collider.GetComponentInParent<ScenePortal>();
    }

    void ShowSwoosh(Vector2 center, Vector2 direction, ToolCombatProfile profile)
    {
        if (!profile.showSwoosh || !profile.swooshSheet || profile.swooshFrameHeight <= 0) return;

        if (activeSwoosh) Destroy(activeSwoosh);

        var swing = new GameObject("Melee Swoosh");
        activeSwoosh = swing;
        swing.transform.position = center;
        swing.transform.right = direction;
        swing.transform.localScale = Vector3.one * profile.swooshScale;
        var renderer = swing.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100) + 10;
        int frameCount = profile.swooshSheet.height / profile.swooshFrameHeight;
        if (frameCount <= 0) { Destroy(swing); return; }
        swing.AddComponent<MeleeSwooshAnimator>().Play(
            profile.swooshSheet, profile.swooshFrameHeight, frameCount, profile.swooshFrameDuration, renderer);
    }

    sealed class MeleeSwooshAnimator : MonoBehaviour
    {
        public void Play(Texture2D sheet, int frameHeight, int frameCount, float frameDuration, SpriteRenderer renderer)
        {
            StartCoroutine(Animate(sheet, frameHeight, frameCount, frameDuration, renderer));
        }

        System.Collections.IEnumerator Animate(Texture2D sheet, int frameHeight, int frameCount, float frameDuration, SpriteRenderer renderer)
        {
            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                float y = (frameCount - frameIndex - 1) * frameHeight;
                var frame = Sprite.Create(
                    sheet, new Rect(0f, y, sheet.width, frameHeight), new Vector2(0f, .5f), PixelArtStandard.PixelsPerUnit);
                Sprite previous = renderer.sprite;
                renderer.sprite = frame;
                if (previous) Destroy(previous);
                yield return new WaitForSeconds(frameDuration);
            }
            if (renderer.sprite) Destroy(renderer.sprite);
            Destroy(gameObject);
        }
    }
}
