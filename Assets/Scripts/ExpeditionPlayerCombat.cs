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
    public Sprite placeholderSprite;

    float nextAttackTime;
    ExpeditionPlayerHealth health;

    void Awake()
    {
        health = GetComponent<ExpeditionPlayerHealth>();
        damage = PlayerProgression.CurrentMeleeDamage;
    }

    void Update()
    {
        if (health && health.IsDefeated) return;
        if (!Input.GetMouseButtonDown(0) || Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + attackCooldown;
        Attacked?.Invoke();
        var camera = Camera.main;
        Vector2 direction = camera
            ? (Vector2)camera.ScreenToWorldPoint(Input.mousePosition) - (Vector2)transform.position
            : Vector2.right;
        if (direction.sqrMagnitude < .01f) direction = Vector2.right;
        direction.Normalize();

        Vector2 center = (Vector2)transform.position + direction * attackRange;
        foreach (var hit in Physics2D.OverlapCircleAll(center, attackRadius))
        {
            var enemy = hit.GetComponentInParent<WildernessEnemy>();
            if (enemy) enemy.TakeDamage(damage, transform.position, knockbackDistance);
        }
        ShowPlaceholderSwing(center, direction);
    }

    void ShowPlaceholderSwing(Vector2 center, Vector2 direction)
    {
        if (!placeholderSprite) return;
        var swing = new GameObject("Melee Swing");
        swing.transform.position = center;
        swing.transform.right = direction;
        swing.transform.localScale = new Vector3(1.15f, .42f, 1f);
        var renderer = swing.AddComponent<SpriteRenderer>();
        renderer.sprite = placeholderSprite;
        renderer.color = new Color(1f, .78f, .25f, .72f);
        renderer.sortingOrder = 10000;
        Destroy(swing, .1f);
    }
}
