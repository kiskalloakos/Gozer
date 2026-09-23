using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
public class WildernessEnemy : MonoBehaviour
{
    enum EnemyState { Wandering, Alerting, Pursuing, AttackWindup, Recovering }

    [Min(.1f)] public float wanderSpeed = .75f;
    [Min(.1f)] public float pursuitSpeed = 2.35f;
    [Min(.1f)] public float detectionRadius = 7f;
    [Min(.1f)] public float attackRange = 1.05f;
    [Min(.1f)] public float attackWindupSeconds = .48f;
    [Min(.1f)] public float attackRecoverySeconds = .9f;
    // Player health uses half-heart units, so the default hit removes half a heart.
    [Min(1)] public int contactDamage = 1;
    // Health and damage are measured in half-hearts (6 units = 3 hearts).
    [Min(1)] public int maxHealth = 6;
    [Min(0)] public int lootDropAmount = 1;

    Rigidbody2D body;
    SpriteRenderer visual;
    Transform player;
    ExpeditionPlayerHealth playerHealth;
    DemonSpriteAnimator spriteAnimator;
    EnemyState state;
    Vector2 wanderDirection;
    Vector2 home;
    float stateUntil;
    float hitFlashUntil;
    float healthBarUntil;
    int currentHealth;
    Color baseColor = Color.white;
    Vector3 baseScale;
    bool runDifficultyApplied;
    bool threatHealthApplied;
    bool dying;
    bool alternateAttack;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        visual = GetComponent<SpriteRenderer>();
        spriteAnimator = GetComponent<DemonSpriteAnimator>();
        baseScale = transform.localScale;
        // Older scene instances serialized 3, when this value represented
        // three hits rather than three full hearts.
        maxHealth = Mathf.Max(6, maxHealth);
        currentHealth = maxHealth;
        home = body.position;
        ChooseWanderDirection();
        FindPlayer();
        // Existing expedition scenes gain the Demon_A presentation without a
        // scene rebuild; newly generated enemies receive this in the setup too.
        if (!spriteAnimator) spriteAnimator = gameObject.AddComponent<DemonSpriteAnimator>();
    }

    void Update()
    {
        if (dying)
        {
            visual.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
            return;
        }
        if (!player) FindPlayer();
        if (!player || (playerHealth && playerHealth.IsDefeated)) return;

        float distance = Vector2.Distance(body.position, player.position);
        bool showingHitFlash = Time.time < hitFlashUntil;
        switch (state)
        {
            case EnemyState.Wandering:
                if (!showingHitFlash) visual.color = baseColor;
                if (distance <= detectionRadius) BeginAlert();
                else if (Time.time >= stateUntil) ChooseWanderDirection();
                break;
            case EnemyState.Alerting:
                if (!showingHitFlash) visual.color = new Color(1f, .88f, .68f);
                if (Time.time >= stateUntil) state = EnemyState.Pursuing;
                break;
            case EnemyState.Pursuing:
                if (!showingHitFlash) visual.color = new Color(1f, .74f, .74f);
                if (distance <= attackRange) BeginAttack();
                break;
            case EnemyState.AttackWindup:
                if (!showingHitFlash)
                    visual.color = Color.Lerp(new Color(1f, .15f, .1f), Color.white,
                        Mathf.PingPong(Time.time * 8f, 1f));
                transform.localScale = baseScale * (1f + Mathf.PingPong(Time.time * 4f, .16f));
                if (Time.time >= stateUntil) FinishAttack();
                break;
            case EnemyState.Recovering:
                if (!showingHitFlash) visual.color = new Color(.78f, .78f, .78f);
                if (Time.time >= stateUntil) state = EnemyState.Pursuing;
                break;
        }
        visual.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
    }

    void FixedUpdate()
    {
        if (!player) return;
        Vector2 velocity = Vector2.zero;
        if (state == EnemyState.Wandering)
        {
            if (Vector2.Distance(body.position, home) > 5f)
                wanderDirection = (home - body.position).normalized;
            velocity = wanderDirection * wanderSpeed;
        }
        else if (state == EnemyState.Pursuing)
            velocity = ((Vector2)player.position - body.position).normalized * pursuitSpeed;

        body.MovePosition(body.position + velocity * Time.fixedDeltaTime);
    }

    void FindPlayer()
    {
        var target = FindAnyObjectByType<ExpeditionPlayerHealth>();
        if (!target) return;
        playerHealth = target;
        player = target.transform;
    }

    void ChooseWanderDirection()
    {
        wanderDirection = Random.insideUnitCircle.normalized;
        stateUntil = Time.time + Random.Range(1.2f, 3.2f);
        state = EnemyState.Wandering;
    }

    void BeginAlert()
    {
        if (state == EnemyState.Alerting || state == EnemyState.Pursuing || state == EnemyState.AttackWindup) return;
        state = EnemyState.Alerting;
        stateUntil = Time.time + .55f;
    }

    void BeginAttack()
    {
        state = EnemyState.AttackWindup;
        stateUntil = Time.time + attackWindupSeconds;
        if (spriteAnimator) spriteAnimator.PlayAttack(alternateAttack = !alternateAttack);
    }

    void FinishAttack()
    {
        transform.localScale = baseScale;
        if (playerHealth && Vector2.Distance(body.position, player.position) <= attackRange + .35f)
            playerHealth.TakeDamage(contactDamage, body.position);
        state = EnemyState.Recovering;
        stateUntil = Time.time + attackRecoverySeconds;
    }

    public bool TakeDamage(int amount, Vector2 attackerPosition, float knockbackDistance)
    {
        if (amount <= 0 || dying) return false;
        currentHealth -= amount;
        healthBarUntil = Time.time + 3f;
        hitFlashUntil = Time.time + .1f;
        if (spriteAnimator) spriteAnimator.PlayHurt();
        transform.localScale = baseScale;

        Vector2 away = body.position - attackerPosition;
        // If the hit lands while the two transforms overlap, use the demon's
        // current facing instead of pushing every ambiguous hit to the right.
        if (away.sqrMagnitude < .01f)
            away = spriteAnimator ? spriteAnimator.FacingDirection : Vector2.left;
        body.position += away.normalized * Mathf.Max(0f, knockbackDistance);
        Physics2D.SyncTransforms();

        if (currentHealth <= 0)
        {
            dying = true;
            state = EnemyState.Recovering;
            body.simulated = false;
            var collider = GetComponent<Collider2D>();
            if (collider) collider.enabled = false;
            if (spriteAnimator) spriteAnimator.PlayDeath(FinalizeDeath);
            else FinalizeDeath();
            return true;
        }

        // A successful melee hit interrupts the current attack windup and gives
        // the player a short window to reposition or follow up.
        state = EnemyState.Alerting;
        stateUntil = Time.time + .2f;
        return true;
    }

    void FinalizeDeath()
    {
        ExpeditionLootPickup.Spawn(transform.position, lootDropAmount);
        Destroy(gameObject);
    }

    public void AlertFromExtraction()
    {
        if (state == EnemyState.AttackWindup) return;
        state = EnemyState.Alerting;
        stateUntil = Time.time + .35f;
    }

    public void Relocate(Vector2 position)
    {
        if (!body) body = GetComponent<Rigidbody2D>();
        body.position = position;
        home = position;
    }

    public void ApplyRunDifficulty(int completedRuns)
    {
        ApplyThreatHealth(completedRuns);
        if (runDifficultyApplied || completedRuns <= 0) return;
        runDifficultyApplied = true;

        float difficultyGrowth = 1f - Mathf.Exp(-completedRuns * .08f);
        float speedMultiplier = 1f + .5f * difficultyGrowth;
        wanderSpeed *= speedMultiplier;
        pursuitSpeed *= speedMultiplier;
        detectionRadius += 4f * difficultyGrowth;
        attackWindupSeconds *= .6f + .4f * Mathf.Exp(-completedRuns * .06f);
    }

    public void ApplyThreatHealth(int completedRuns)
    {
        if (threatHealthApplied) return;
        threatHealthApplied = true;
        // Each threat level adds half a heart, starting at three hearts.
        maxHealth = Mathf.Max(6, maxHealth) + Mathf.Max(0, completedRuns);
        currentHealth = maxHealth;
    }

    public static void AlertAllFromExtraction()
    {
        foreach (var enemy in FindObjectsByType<WildernessEnemy>())
            enemy.AlertFromExtraction();
    }

    void OnGUI()
    {
        if (Time.time >= healthBarUntil && state != EnemyState.Alerting && state != EnemyState.AttackWindup) return;
        var camera = Camera.main;
        if (!camera) return;
        Vector3 screen = camera.WorldToScreenPoint(transform.position + Vector3.up * 1.1f);
        if (screen.z < 0) return;

        if (Time.time < healthBarUntil)
        {
            var previousColor = GUI.color;
            var bar = new Rect(screen.x - 27f, Screen.height - screen.y - 31f, 54f, 7f);
            GUI.color = new Color(.12f, .1f, .1f, .9f);
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = new Color(.92f, .24f, .22f);
            GUI.DrawTexture(new Rect(bar.x + 1f, bar.y + 1f,
                (bar.width - 2f) * Mathf.Clamp01((float)currentHealth / maxHealth), bar.height - 2f),
                Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        if (state != EnemyState.Alerting && state != EnemyState.AttackWindup) return;

        string message = state == EnemyState.AttackWindup ? "ATTACK!" : "!";
        var style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = state == EnemyState.AttackWindup ? 13 : 24,
            fontStyle = FontStyle.Bold,
            normal = { textColor = state == EnemyState.AttackWindup ? Color.red : new Color(1f, .78f, .2f) }
        };
        GUI.Label(new Rect(screen.x - 42f, Screen.height - screen.y - 20f, 84f, 30f), message, style);
    }
}
