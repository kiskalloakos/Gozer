using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class TreeSpitterEnemy : MonoBehaviour
{
    const float FloatRadius = .7f;
    const float FloatSpeed = .45f;

    [Min(.5f)] public float shotInterval = 2.8f;
    [Min(1f)] public float attackRange = 9f;
    [Min(.1f)] public float projectileSpeed = 3.4f;
    [Min(.5f)] public float projectileLifetime = 5f;
    [Min(0)] public int damageHalfHearts = 2;
    [Min(.5f)] public float projectileScale = 1f;

    public Sprite idleSprite;
    public Sprite attackSprite;
    public Sprite blobSprite;
    SpriteRenderer visual;
    Rigidbody2D body;
    Vector2 home;
    Vector2 driftTarget;
    float nextDriftAt;
    float nextShotAt;
    float attackPoseUntil;
    float driftPausedUntil;
    float bobOffset;

    void Awake()
    {
        visual = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
        home = transform.position;
        driftTarget = home;
        bobOffset = Random.value * Mathf.PI * 2f;
        nextDriftAt = Time.time + Random.Range(.5f, 2f);
        nextShotAt = Time.time + Random.Range(.8f, shotInterval);
        if (idleSprite) visual.sprite = idleSprite;
    }

    void Update()
    {
        visual.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
        Vector2 offset = driftTarget - home;
        if (Time.time >= nextDriftAt || offset.sqrMagnitude > FloatRadius * FloatRadius)
        {
            driftTarget = home + Random.insideUnitCircle * FloatRadius;
            nextDriftAt = Time.time + Random.Range(1.4f, 3f);
        }

        if (Time.time < attackPoseUntil) return;
        if (visual.sprite != idleSprite) visual.sprite = idleSprite;

        if (Time.time < nextShotAt) return;
        ExpeditionPlayerHealth player = FindAnyObjectByType<ExpeditionPlayerHealth>();
        if (!player || player.IsDefeated) return;
        if (Vector2.Distance(transform.position, player.transform.position) > attackRange) return;

        FireAt(player.transform.position);
        nextShotAt = Time.time + shotInterval;
    }

    void FixedUpdate()
    {
        if (!body || Time.time < driftPausedUntil) return;
        Vector2 position = Vector2.MoveTowards(body.position, driftTarget, FloatSpeed * Time.fixedDeltaTime);
        position.y += Mathf.Sin(Time.time * 1.5f + bobOffset) * .1f * Time.fixedDeltaTime;
        body.MovePosition(position);
    }

    void FireAt(Vector2 target)
    {
        if (attackSprite) visual.sprite = attackSprite;
        attackPoseUntil = Time.time + .25f;
        if (!blobSprite) return;

        Vector2 direction = (target - (Vector2)transform.position).normalized;
        if (direction.sqrMagnitude < .001f) direction = Vector2.down;
        TreeSpitProjectile.Spawn(blobSprite,
            (Vector2)transform.position + direction * .38f,
            direction, projectileSpeed, projectileLifetime, damageHalfHearts, projectileScale);
    }

    public void Relocate(Vector2 position)
    {
        home = position;
        driftTarget = position;
        if (body) body.position = position;
        transform.position = position;
        Physics2D.SyncTransforms();
    }

    public void OnKnockedBack(Vector2 position)
    {
        home = position;
        driftTarget = position;
        driftPausedUntil = Time.time + .2f;
        attackPoseUntil = 0f;
        nextShotAt = Mathf.Max(nextShotAt, driftPausedUntil);
        if (body) body.position = position;
        transform.position = position;
        Physics2D.SyncTransforms();
    }
}

public sealed class TreeSpitProjectile : MonoBehaviour
{
    Vector2 velocity;
    float expiresAt;
    int damage;
    bool spent;
    Rigidbody2D body;

    public static void Spawn(Sprite sprite, Vector2 position, Vector2 direction,
        float speed, float lifetime, int damageHalfHearts, float scale)
    {
        var projectile = new GameObject("Enemy 2 Spit Blob");
        projectile.transform.position = position;
        projectile.transform.localScale = Vector3.one * scale;

        var renderer = projectile.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = Mathf.RoundToInt(-position.y * 100f) + 2;

        var collider = projectile.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = .28f;

        var body = projectile.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var behavior = projectile.AddComponent<TreeSpitProjectile>();
        behavior.velocity = direction.normalized * speed;
        behavior.expiresAt = Time.time + lifetime;
        behavior.damage = damageHalfHearts;
        behavior.body = body;
    }

    void Update()
    {
        if (Time.time >= expiresAt)
        {
            Destroy(gameObject);
            return;
        }

    }

    void FixedUpdate()
    {
        if (!spent && body)
            body.MovePosition(body.position + velocity * Time.fixedDeltaTime);
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer) renderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) + 2;
    }

    public bool HitByAttack()
    {
        if (spent) return false;
        spent = true;
        Destroy(gameObject);
        return true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (spent) return;
        var player = other.GetComponentInParent<ExpeditionPlayerHealth>();
        if (!player) return;

        spent = true;
        player.TakeDamage(damage, transform.position);
        Destroy(gameObject);
    }
}
