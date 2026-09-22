using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D))]
public class ExpeditionPlayerHealth : MonoBehaviour
{
    public const string InjuryKey = "Player.Injured";
    public const string HealthKey = "Player.HealthHalfHearts";
    public const int DefaultMaxHearts = 5;
    public const int DefaultMaxHealthUnits = DefaultMaxHearts * 2;

    [FormerlySerializedAs("maxHealth")]
    [Min(1)] public int maxHearts = DefaultMaxHearts;
    [Min(.1f)] public float invulnerabilitySeconds = .8f;
    [Min(0f)] public float knockbackDistance = .75f;

    // Health is stored in half-heart units: 10 units equals five full hearts.
    public int CurrentHealth { get; private set; }
    public int MaxHealthUnits => maxHearts * 2;
    public bool IsInvulnerable => Time.time < invulnerableUntil;
    public bool IsDefeated { get; private set; }

    float invulnerableUntil;
    SpriteRenderer visual;
    TownPlayerController movement;
    ExpeditionPlayerCombat combat;
    ExpeditionHUD inventory;

    void Awake()
    {
        if (!PlayerPrefs.HasKey(HealthKey))
        {
            bool previouslyInjured = PlayerPrefs.GetInt(InjuryKey, 0) == 1;
            PlayerPrefs.SetInt(HealthKey, previouslyInjured ? 0 : MaxHealthUnits);
            PlayerPrefs.Save();
        }
        CurrentHealth = Mathf.Clamp(PlayerPrefs.GetInt(HealthKey, MaxHealthUnits), 0, MaxHealthUnits);
        visual = GetComponentInChildren<SpriteRenderer>();
        movement = GetComponent<TownPlayerController>();
        combat = GetComponent<ExpeditionPlayerCombat>();
        inventory = GetComponent<ExpeditionHUD>();
    }

    void Update()
    {
        if (!visual || IsDefeated) return;
        visual.enabled = !IsInvulnerable || Mathf.FloorToInt(Time.time * 14f) % 2 == 0;
    }

    public void TakeDamage(int amount, Vector2 attackerPosition)
    {
        if (amount <= 0 || IsInvulnerable || IsDefeated) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        PlayerPrefs.SetInt(HealthKey, CurrentHealth);
        PlayerPrefs.Save();
        invulnerableUntil = Time.time + invulnerabilitySeconds;
        ApplyKnockback(attackerPosition);

        if (CurrentHealth == 0) StartCoroutine(ReturnToTownInjured());
    }

    void ApplyKnockback(Vector2 attackerPosition)
    {
        var body = GetComponent<Rigidbody2D>();
        Vector2 direction = body.position - attackerPosition;
        if (direction.sqrMagnitude < .01f) direction = Vector2.down;
        body.position += direction.normalized * knockbackDistance;
        Physics2D.SyncTransforms();
    }

    IEnumerator ReturnToTownInjured()
    {
        IsDefeated = true;
        int lostGold = inventory ? inventory.CarriedLoot : 0;
        ExpeditionRunResult.RecordDefeat(lostGold, CurrentHealth);
        if (inventory) inventory.LoseLoot();
        if (movement) movement.enabled = false;
        if (combat) combat.enabled = false;
        if (visual) visual.enabled = true;

        PlayerPrefs.SetInt(InjuryKey, 1);
        PlayerPrefs.SetInt(HealthKey, 0);
        PlayerPrefs.Save();
        yield return new WaitForSeconds(1.1f);
        SceneTravel.Load(GameScene.TownHub, SceneSpawnPoint.TownExpeditionGate);
    }

    void OnGUI()
    {
        if (!IsDefeated) return;
        var style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, .35f, .3f) }
        };
        GUI.Label(new Rect(0, Screen.height / 2f - 35f, Screen.width, 70f), "WOUNDED — RETREATING TO TOWN", style);
    }
}
