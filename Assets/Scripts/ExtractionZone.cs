using UnityEngine;

public class ExtractionZone : MonoBehaviour
{
    [Min(.1f)] public float extractionSeconds = 10f;
    public string destinationScene = "TownHub";
    public string destinationSpawnId = SceneTravel.TownExpeditionGate;

    float elapsed;
    bool playerInside;
    bool travelStarted;
    bool cancelledUntilExit;
    float cancellationMessageUntil;
    ExpeditionPlayerCombat playerCombat;

    public float RemainingSeconds => Mathf.Max(0f, extractionSeconds - elapsed);

    void Update()
    {
        if (!playerInside || travelStarted) return;

        elapsed += Time.deltaTime;
        if (elapsed < extractionSeconds) return;

        travelStarted = true;
        var inventory = FindAnyObjectByType<ExpeditionHUD>();
        if (inventory) inventory.SecureLoot();
        SceneTravel.Load(destinationScene, destinationSpawnId);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.GetComponentInParent<TownPlayerController>()) return;
        if (cancelledUntilExit) return;

        StopListeningForAttacks();
        playerCombat = other.GetComponentInParent<ExpeditionPlayerCombat>();
        if (playerCombat) playerCombat.Attacked += CancelForAttack;
        if (!playerInside)
        {
            ExpeditionDifficultyDirector.SpawnExtractionReinforcements();
            WildernessEnemy.AlertAllFromExtraction();
        }
        playerInside = true;
        elapsed = 0f;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.GetComponentInParent<TownPlayerController>()) return;
        playerInside = false;
        cancelledUntilExit = false;
        elapsed = 0f;
        StopListeningForAttacks();
    }

    void OnDisable()
    {
        StopListeningForAttacks();
    }

    void CancelForAttack()
    {
        if (!playerInside || travelStarted) return;
        playerInside = false;
        cancelledUntilExit = true;
        elapsed = 0f;
        cancellationMessageUntil = Time.time + 1.5f;
    }

    void StopListeningForAttacks()
    {
        if (playerCombat) playerCombat.Attacked -= CancelForAttack;
        playerCombat = null;
    }

    void OnGUI()
    {
        if (travelStarted) return;

        var title = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 22,
            fontStyle = FontStyle.Bold
        };
        var hint = new GUIStyle(title) { fontSize = 14, fontStyle = FontStyle.Normal };
        bool showingCancellation = Time.time < cancellationMessageUntil;
        if (!playerInside && !showingCancellation) return;

        var panel = new Rect(Screen.width / 2f - 180f, 24f, 360f, 76f);

        var oldColor = GUI.color;
        GUI.color = new Color(.08f, .055f, .04f, .94f);
        GUI.Box(panel, GUIContent.none);
        GUI.color = showingCancellation ? new Color(1f, .3f, .22f) : new Color(1f, .78f, .28f);
        GUI.Label(new Rect(panel.x + 10f, panel.y + 7f, panel.width - 20f, 34f),
            showingCancellation ? "EXTRACTION CANCELLED" : $"EXTRACTING — {RemainingSeconds:0.0}s", title);
        GUI.color = Color.white;
        GUI.Label(new Rect(panel.x + 10f, panel.y + 40f, panel.width - 20f, 24f),
            showingCancellation ? "Leave and re-enter to try again" : "Attacking or leaving cancels extraction", hint);
        GUI.color = oldColor;
    }
}
