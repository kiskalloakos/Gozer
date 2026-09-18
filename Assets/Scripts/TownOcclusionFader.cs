using UnityEngine;

/// <summary>
/// Fades tall town scenery while the player is walking behind its artwork.
/// The object keeps its collision and depth sorting; only its visible pixels
/// become translucent so the hidden player remains readable.
/// </summary>
public class TownOcclusionFader : MonoBehaviour
{
    [Range(0.1f, 1f)] public float occludedAlpha = .5f;
    public float fadeSpeed = 4f;
    public float horizontalPadding = .1f;
    public float bottomPadding = .1f;

    SpriteRenderer[] renderers;
    Color[] baseColors;
    Transform player;

    void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;

        var controller = FindAnyObjectByType<TownPlayerController>();
        if (controller) player = controller.transform;
    }

    void LateUpdate()
    {
        if (!player || renderers == null || renderers.Length == 0) return;

        bool hasBounds = false;
        var combined = new Bounds();
        foreach (var renderer in renderers)
        {
            if (!renderer || !renderer.enabled || !renderer.sprite) continue;
            if (!hasBounds) { combined = renderer.bounds; hasBounds = true; }
            else combined.Encapsulate(renderer.bounds);
        }
        if (!hasBounds) return;

        Vector3 position = player.position;
        bool horizontallyCovered = position.x > combined.min.x - horizontalPadding
            && position.x < combined.max.x + horizontalPadding;
        bool behindArtwork = position.y > combined.min.y + bottomPadding
            && position.y < combined.max.y;
        float alpha = horizontallyCovered && behindArtwork ? occludedAlpha : 1f;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (!renderers[i]) continue;
            var color = renderers[i].color;
            color.a = Mathf.MoveTowards(color.a, baseColors[i].a * alpha, fadeSpeed * Time.deltaTime);
            renderers[i].color = color;
        }
    }

    void OnDisable()
    {
        if (renderers == null || baseColors == null) return;
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i]) renderers[i].color = baseColors[i];
    }
}
