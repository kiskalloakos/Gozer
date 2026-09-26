using System.Collections;
using UnityEngine;

public sealed class HouseSceneFade : MonoBehaviour
{
    const float FadeOutSeconds = .35f;
    const float FadeInSeconds = .45f;
    float darkness;
    static HouseSceneFade activeFade;
    GameScene destination;
    SceneSpawnPoint spawn;

    public static Color FadeColor(Color color)
    {
        if (activeFade) color.a *= 1f - activeFade.darkness;
        return color;
    }

    public void Begin(GameScene scene, SceneSpawnPoint spawnPoint)
    {
        destination = scene;
        spawn = spawnPoint;
        StartCoroutine(Travel());
    }

    IEnumerator Travel()
    {
        yield return Fade(0f, 1f, FadeOutSeconds);
        yield return SceneTravel.LoadHouseScene(destination, spawn);
        yield return null; // Let the destination camera render before revealing it.
        yield return Fade(1f, 0f, FadeInSeconds);
        Destroy(gameObject);
    }

    IEnumerator Fade(float from, float to, float seconds)
    {
        float elapsed = 0f;
        darkness = from;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            darkness = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / seconds)));
            yield return null;
        }
        darkness = to;
    }

    void Awake() => activeFade = this;

    void OnGUI()
    {
        if (darkness <= 0f) return;
        int previousDepth = GUI.depth;
        Color previousColor = GUI.color;
        GUI.depth = -10001;
        GUI.color = new Color(0f, 0f, 0f, darkness);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previousColor;
        GUI.depth = previousDepth;
    }

    void OnDestroy()
    {
        if (activeFade == this) activeFade = null;
        SceneTravel.FinishTransition();
    }
}
