using UnityEngine;

public sealed class RunResultScreen : MonoBehaviour
{
    ExpeditionRunSummary summary;
    Texture2D box;
    float shownAt;
    bool visible;

    public void Show(ExpeditionRunSummary value)
    {
        summary = value;
        box = Resources.Load<Texture2D>("UI/textbox");
        shownAt = Time.unscaledTime;
        visible = true;
    }

    void Update()
    {
        if (visible && Time.unscaledTime - shownAt > 8f) visible = false;
    }

    void OnGUI()
    {
        if (!visible) return;
        float progress = Mathf.Clamp01((Time.unscaledTime - shownAt) / .35f);
        float eased = 1f - Mathf.Pow(1f - progress, 3f);
        float width = Mathf.Min(Screen.width - 32f, 480f);
        float height = width * 100f / 110f;
        float x = (Screen.width - width) * .5f;
        float y = Mathf.Lerp(Screen.height + height, Screen.height * .5f - height * .5f, eased);

        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, .58f * eased);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        if (box) GUI.DrawTexture(new Rect(x, y, width, height), box, ScaleMode.ScaleToFit, true);
        else
        {
            GUI.color = new Color(.08f, .055f, .04f, .97f);
            GUI.Box(new Rect(x, y, width, height), GUIContent.none);
            GUI.color = Color.white;
        }

        GUI.color = summary.succeeded ? new Color(1f, .94f, .68f) : new Color(1f, .48f, .4f);
        ExpeditionHUD.DrawPixelTextAt(summary.succeeded ? "EXPEDITION COMPLETE" : "EXPEDITION FAILED", x + width * .5f, y + height * .20f, 2f);
        GUI.color = new Color(1f, .94f, .68f);
        ExpeditionHUD.DrawPixelTextAt(summary.succeeded
            ? $"SECURED GOLD  {summary.securedGold}"
            : $"LOST GOLD  {summary.lostGold}", x + width * .5f, y + height * .43f, 2f);
        ExpeditionHUD.DrawPixelTextAt($"HEALTH  {summary.healthUnits / 2f:0.0} HEARTS", x + width * .5f, y + height * .60f, 2f);
        if (summary.succeeded && summary.nextThreat > 0)
            ExpeditionHUD.DrawPixelTextAt($"NEXT THREAT  {summary.nextThreat}", x + width * .5f, y + height * .77f, 2f);
        GUI.color = oldColor;
    }
}
