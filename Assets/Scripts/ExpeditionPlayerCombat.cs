using System;
using UnityEngine;

public class ExpeditionPlayerCombat : MonoBehaviour
{
    public event Action Attacked;

    [Min(.1f)] public float attackRange = 1.15f;
    [Min(.1f)] public float attackRadius = .7f;
    [Min(.05f)] public float attackCooldown = .38f;
    [Min(1)] public int damage = 1;
    [Min(0f)] public float knockbackDistance = 1.15f;
    [Header("Attack effect")]
    public Texture2D swooshSheet;
    [Min(.01f)] public float swooshFrameDuration = .055f;
    [Min(.1f)] public float swooshScale = .25f;
    [Min(0f)] public float swooshHeightAboveFeet = 1f;
    [Min(1)] public int swooshFrameHeight = 32;

    float nextAttackTime;
    ExpeditionPlayerHealth health;
    TownPlayerController movementController;
    GameObject activeSwoosh;

    void Awake()
    {
        health = GetComponent<ExpeditionPlayerHealth>();
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
        Vector2 facing = movementController ? movementController.FacingVector : Vector2.down;
        Vector2 direction = camera
            ? (Vector2)camera.ScreenToWorldPoint(Input.mousePosition) - (Vector2)transform.position
            : facing;
        if (direction.sqrMagnitude < .01f) direction = facing;
        direction.Normalize();

        // Aim freely inside the forward-facing half of the player, but do not
        // start an attack when the cursor is behind the character.
        if (Vector2.Dot(direction, facing) < 0f) return;

        nextAttackTime = Time.time + attackCooldown;
        Attacked?.Invoke();

        // A short, narrow sweep selects only the first enemy in the aimed
        // direction. The visual arc is not an area-of-effect attack.
        WildernessEnemy target = null;
        float nearest = float.PositiveInfinity;
        foreach (var hit in Physics2D.CircleCastAll(transform.position, attackRadius * .5f,
                     direction, attackRange + attackRadius * .5f))
        {
            var enemy = hit.collider ? hit.collider.GetComponentInParent<WildernessEnemy>() : null;
            if (!enemy || Vector2.Dot((Vector2)enemy.transform.position - (Vector2)transform.position,
                    direction) <= 0f || hit.distance >= nearest) continue;
            target = enemy;
            nearest = hit.distance;
        }
        if (target) target.TakeDamage(damage, transform.position, knockbackDistance);
        if (movementController) movementController.PlayMeleeAttack(direction);
        // The player transform is positioned at their feet. Lift the visual to
        // weapon height so it does not appear on the ground beneath their legs.
        Vector2 visualOrigin = (Vector2)transform.position + Vector2.up * swooshHeightAboveFeet;
        ShowSwoosh(visualOrigin + direction * .1f, direction);
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
