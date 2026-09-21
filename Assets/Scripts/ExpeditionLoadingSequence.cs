using System.Collections;
using UnityEngine;

public sealed class ExpeditionLoadingSequence : MonoBehaviour
{
    public const float MinimumDuration = 5f;
    const float RevealStart = 2.6f;
    const float TargetCameraSize = PixelArtStandard.OrthographicSize;

    Camera viewCamera;
    FollowCamera followCamera;
    Transform player;
    Vector3 overviewPosition;
    float overviewSize;
    float elapsed;
    float previousTimeScale;
    int expeditionSeed;
    int threatLevel;
    TownPlayerController playerMovement;
    ExpeditionPlayerCombat playerCombat;
    ExpeditionFieldOfView fieldOfView;
    bool movementWasEnabled;
    bool combatWasEnabled;
    bool running;

    public static void Begin(Transform player, int halfWidth, int halfHeight, int seed)
    {
        var camera = Camera.main;
        if (!camera || !player) return;

        var sequence = camera.GetComponent<ExpeditionLoadingSequence>();
        if (!sequence) sequence = camera.gameObject.AddComponent<ExpeditionLoadingSequence>();
        sequence.StartSequence(player, halfWidth, halfHeight, seed);
    }

    void StartSequence(Transform target, int halfWidth, int halfHeight, int seed)
    {
        viewCamera = GetComponent<Camera>();
        followCamera = GetComponent<FollowCamera>();
        player = target;
        elapsed = 0f;
        expeditionSeed = seed;
        threatLevel = seed == 0 ? 0 : ExpeditionRunProgression.ThreatLevel;
        running = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        if (followCamera) followCamera.enabled = false;
        playerMovement = player.GetComponent<TownPlayerController>();
        playerCombat = player.GetComponent<ExpeditionPlayerCombat>();
        if (playerMovement)
        {
            movementWasEnabled = playerMovement.enabled;
            playerMovement.enabled = false;
        }
        if (playerCombat)
        {
            combatWasEnabled = playerCombat.enabled;
            playerCombat.enabled = false;
        }

        float aspect = Mathf.Max(.1f, viewCamera.aspect);
        overviewSize = CalculateOverviewSize(halfWidth, halfHeight, aspect);
        overviewPosition = new Vector3(0f, 0f, -10f);
        viewCamera.transform.position = overviewPosition;
        viewCamera.orthographicSize = overviewSize;
        Debug.Log($"Expedition loading reveal started for seed {seed}.");
        StartCoroutine(Run());
    }

    public static float CalculateOverviewSize(int halfWidth, int halfHeight, float aspect)
    {
        return Mathf.Max(halfHeight + 1.5f, (halfWidth + 1.5f) / Mathf.Max(.1f, aspect));
    }

    IEnumerator Run()
    {
        while (elapsed < MinimumDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float zoomAmount = EaseIntoArrival(
                Mathf.InverseLerp(RevealStart, MinimumDuration, elapsed));
            if (!fieldOfView) fieldOfView = GetComponent<ExpeditionFieldOfView>();
            if (fieldOfView) fieldOfView.SetRevealProgress(zoomAmount);
            Vector3 targetPosition = new Vector3(player.position.x, player.position.y, -10f);
            viewCamera.transform.position = Vector3.Lerp(overviewPosition, targetPosition, zoomAmount);
            viewCamera.orthographicSize = Mathf.Lerp(overviewSize, TargetCameraSize, zoomAmount);
            yield return null;
        }

        viewCamera.transform.position = new Vector3(player.position.x, player.position.y, -10f);
        viewCamera.orthographicSize = TargetCameraSize;
        if (fieldOfView) fieldOfView.SetRevealProgress(1f);
        if (followCamera) followCamera.enabled = true;
        RestorePlayerControls();
        Time.timeScale = previousTimeScale;
        running = false;
    }

    void OnDisable()
    {
        if (!running) return;
        if (fieldOfView) fieldOfView.SetRevealProgress(1f);
        RestorePlayerControls();
        Time.timeScale = previousTimeScale;
        running = false;
    }

    void RestorePlayerControls()
    {
        if (playerMovement) playerMovement.enabled = movementWasEnabled;
        if (playerCombat) playerCombat.enabled = combatWasEnabled;
    }

    static float EaseIntoArrival(float amount)
    {
        // SmoothStep keeps departure gentle. Squaring its remaining distance
        // adds stronger deceleration near the player without extending loading.
        float smooth = amount * amount * (3f - 2f * amount);
        float remaining = 1f - smooth;
        return 1f - remaining * remaining;
    }

    void OnGUI()
    {
        if (!running) return;
        GUI.depth = -1000;
        float brightness = EaseIntoArrival(
            Mathf.InverseLerp(RevealStart, MinimumDuration, elapsed));
        float coverAlpha = 1f - brightness;
        if (coverAlpha > 0f)
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, coverAlpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = oldColor;
        }

        string dots = new string('.', Mathf.FloorToInt(elapsed * 2f) % 4);
        var style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        float width = 150f;
        float height = threatLevel > 0 ? 54f : 38f;
        string seedLabel = expeditionSeed == 0 ? "FIXED FIELD" : $"SEED {expeditionSeed}";
        string threatLabel = threatLevel > 0 ? $"\nTHREAT {threatLevel}" : "";
        GUI.Box(new Rect(Screen.width - width - 22f, Screen.height - height - 20f, width, height),
            $"LOADING{dots}\n{seedLabel}{threatLabel}", style);
    }
}
