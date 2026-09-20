using UnityEngine;

public class HomeInteriorController : MonoBehaviour
{
    public static HomeInteriorController Instance { get; private set; }

    private string notice = "";
    private float noticeUntil;

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void ShowNotice(string message, float seconds = 3.5f)
    {
        notice = message;
        noticeUntil = Time.time + seconds;
    }

    void OnGUI()
    {
        var oldColor = GUI.color;
        var hint = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.UpperCenter,
            wordWrap = true,
            normal = { textColor = new Color(.92f, .9f, .82f, .78f) }
        };

        if (Time.time < noticeUntil)
        {
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width / 2f - 280f, 10f, 560f, 42f), notice, hint);
        }
        GUI.color = oldColor;
    }
}
