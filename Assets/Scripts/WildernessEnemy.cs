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
    [Min(1)] public int maxHealth = 3;
    [Min(0)] public int lootDropAmount = 1;

    Rigidbody2D body;
    SpriteRenderer visual;
    Transform player;
    ExpeditionPlayerHealth playerHealth;
    EnemyState state;
    Vector2 wanderDirection;
    Vector2 home;
    float stateUntil;
    float hitFlashUntil;
    int currentHealth;
    Color baseColor = new Color(.48f, .16f, .22f);

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        visual = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
        home = body.position;
        ChooseWanderDirection();
        FindPlayer();
    }

    void Update()
    {
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
                if (!showingHitFlash) visual.color = new Color(1f, .72f, .18f);
                if (Time.time >= stateUntil) state = EnemyState.Pursuing;
                break;
            case EnemyState.Pursuing:
                if (!showingHitFlash) visual.color = new Color(.9f, .24f, .16f);
                if (distance <= attackRange) BeginAttack();
                break;
            case EnemyState.AttackWindup:
                if (!showingHitFlash)
                    visual.color = Color.Lerp(new Color(1f, .15f, .1f), Color.white,
                        Mathf.PingPong(Time.time * 8f, 1f));
                transform.localScale = Vector3.one * (1f + Mathf.PingPong(Time.time * 4f, .16f));
                if (Time.time >= stateUntil) FinishAttack();
                break;
            case EnemyState.Recovering:
                if (!showingHitFlash) visual.color = new Color(.42f, .12f, .16f);
                if (Time.time >= stateUntil) state = EnemyState.Pursuing;
                break;
        }
        if (showingHitFlash) visual.color = Color.white;
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
    }

    void FinishAttack()
    {
        transform.localScale = Vector3.one;
        if (playerHealth && Vector2.Distance(body.position, player.position) <= attackRange + .35f)
            playerHealth.TakeDamage(contactDamage, body.position);
        state = EnemyState.Recovering;
        stateUntil = Time.time + attackRecoverySeconds;
    }

    public void TakeDamage(int amount, Vector2 attackerPosition, float knockbackDistance)
    {
        if (amount <= 0) return;
        currentHealth -= amount;
        hitFlashUntil = Time.time + .1f;
        transform.localScale = Vector3.one;

        Vector2 away = body.position - attackerPosition;
        if (away.sqrMagnitude < .01f) away = Vector2.right;
        body.position += away.normalized * Mathf.Max(0f, knockbackDistance);
        Physics2D.SyncTransforms();

        if (currentHealth <= 0)
        {
            ExpeditionLootPickup.Spawn(body.position, lootDropAmount);
            Destroy(gameObject);
            return;
        }

        // A successful melee hit interrupts the current attack windup and gives
        // the player a short window to reposition or follow up.
        state = EnemyState.Alerting;
        stateUntil = Time.time + .2f;
    }

    public void AlertFromExtraction()
    {
        if (state == EnemyState.AttackWindup) return;
        state = EnemyState.Alerting;
        stateUntil = Time.time + .35f;
    }

    public static void AlertAllFromExtraction()
    {
        foreach (var enemy in FindObjectsByType<WildernessEnemy>(FindObjectsSortMode.None))
            enemy.AlertFromExtraction();
    }

    void OnGUI()
    {
        if (state != EnemyState.Alerting && state != EnemyState.AttackWindup) return;
        var camera = Camera.main;
        if (!camera) return;
        Vector3 screen = camera.WorldToScreenPoint(transform.position + Vector3.up * 1.1f);
        if (screen.z < 0) return;

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
