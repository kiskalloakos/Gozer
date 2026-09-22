using System;
using System.Collections.Generic;
using UnityEngine;

public class ExpeditionPlayerCombat : MonoBehaviour
{
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

    float nextAttackTime;
    ExpeditionPlayerHealth health;
    ExpeditionPlayerEnergy energy;
    TownPlayerController movementController;
    GameObject activeSwoosh;

    void Awake()
    {
        health = GetComponent<ExpeditionPlayerHealth>();
        energy = GetComponent<ExpeditionPlayerEnergy>();
        if (!energy) energy = gameObject.AddComponent<ExpeditionPlayerEnergy>();
        movementController = GetComponent<TownPlayerController>();
        damage = PlayerProgression.CurrentMeleeDamage;
        // Keep existing expedition scenes working without requiring a manual
        // inspector assignment after this effect is added.
        if (!swooshSheet) swooshSheet = Resources.Load<Texture2D>("Effects/melee_swoosh");
        if (swooshSheet) swooshSheet.filterMode = FilterMode.Point;
    }

    void Update()
    {
        if (GameSessionFlow.IsBlockingGameplay) return;
        if (health && health.IsDefeated) return;
        if (!Input.GetMouseButtonDown(0) || Time.time < nextAttackTime) return;

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

        ChoppableTree treeTarget = null;
        float nearestTree = float.PositiveInfinity;
        float nearestObstacle = float.PositiveInfinity;
        var enemyDistances = new Dictionary<WildernessEnemy, float>();
        foreach (var hit in Physics2D.CircleCastAll(transform.position, attackRadius * .5f,
                     direction, attackRange + attackRadius * .5f))
        {
            if (!hit.collider || hit.collider.transform == transform
                || hit.collider.transform.IsChildOf(transform)) continue;
            var tree = hit.collider ? hit.collider.transform.GetComponentInParent<ChoppableTree>() : null;
            var enemy = hit.collider ? hit.collider.transform.GetComponentInParent<WildernessEnemy>() : null;
            if (!tree && !enemy)
            {
                if (!hit.collider.isTrigger) nearestObstacle = Mathf.Min(nearestObstacle, hit.distance);
                continue;
            }
            Vector2 targetPosition = tree ? (Vector2)tree.transform.position : enemy.transform.position;
            if (Vector2.Dot(targetPosition - (Vector2)transform.position, direction) <= 0f
                || hit.distance >= nearestObstacle) continue;
            if (tree)
            {
                if (hit.distance >= nearestTree) continue;
                treeTarget = tree;
                nearestTree = hit.distance;
            }
            else if (!enemyDistances.TryGetValue(enemy, out float previousDistance)
                     || hit.distance < previousDistance)
                enemyDistances[enemy] = hit.distance;
        }

        // The cast can include the player and several overlapping colliders.
        // Only a solid collider before a target blocks the swing.
        if (nearestObstacle < nearestTree) treeTarget = null;
        WildernessEnemy target = null;
        WildernessEnemy secondTarget = null;
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
        if (treeTarget && nearestTree <= nearestEnemy) { target = null; secondTarget = null; }
        else treeTarget = null;

        // A left click is not an attack by itself. Doors, chests, workbenches,
        // empty ground, and other world interactions must never drain energy.
        if (!treeTarget && !target) return;

        // Trees are resource nodes, not enemies. They can only be chopped with
        // the axe currently equipped in the active quickbar slot. Owning an
        // axe somewhere else in the bag must not make a sword chop trees.
        var hud = FindAnyObjectByType<ExpeditionHUD>();
        if (treeTarget && (!hud || !hud.IsAxeEquipped)) return;

        // Energy is spent only after a valid enemy or tree target is found.
        if (energy && !energy.TryConsume(attackEnergyCost)) return;

        nextAttackTime = Time.time + attackCooldown;
        Attacked?.Invoke();

        // The sword can strike two distinct enemies in the aimed sweep.
        if (treeTarget) treeTarget.TryChop(hud.ActiveQuickbarItem);
        else if (target)
        {
            bool swordEquipped = hud && hud.IsSwordEquipped;
            int attackDamage = damage + (swordEquipped ? swordDamageBonus : 0);
            target.TakeDamage(attackDamage, transform.position, knockbackDistance);
            if (secondTarget && swordEquipped)
                secondTarget.TakeDamage(attackDamage, transform.position, knockbackDistance);
        }
        if (movementController) movementController.PlayMeleeAttack(direction);
        // The player transform is positioned at their feet. Lift the visual to
        // weapon height so it does not appear on the ground beneath their legs.
        Vector2 visualOrigin = (Vector2)transform.position + Vector2.up * swooshHeightAboveFeet;
        ShowSwoosh(visualOrigin + direction * .1f, direction);
    }

    static bool IsTownInteractionUnderCursor(Camera camera)
    {
        Vector2 pointer = camera.ScreenToWorldPoint(Input.mousePosition);
        Collider2D collider = Physics2D.OverlapPoint(pointer);
        if (!collider) return false;
        return collider.GetComponentInParent<TownInteractable>()
            || collider.GetComponentInParent<ScenePortal>();
    }

    void ShowSwoosh(Vector2 center, Vector2 direction)
    {
        if (!swooshSheet || swooshFrameHeight <= 0) return;

        if (activeSwoosh) Destroy(activeSwoosh);

        var swing = new GameObject("Melee Swoosh");
        activeSwoosh = swing;
        swing.transform.position = center;
        swing.transform.right = direction;
        swing.transform.localScale = Vector3.one * swooshScale;
        var renderer = swing.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100) + 10;
        int frameCount = swooshSheet.height / swooshFrameHeight;
        if (frameCount <= 0) { Destroy(swing); return; }
        swing.AddComponent<MeleeSwooshAnimator>().Play(
            swooshSheet, swooshFrameHeight, frameCount, swooshFrameDuration, renderer);
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
