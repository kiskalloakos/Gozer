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
        if (Time.time < noticeUntil)
        {
            GUI.color = Color.white;
            ExpeditionHUD.DrawPixelTextCentered(notice, 10f, 2f);
        }
        GUI.color = oldColor;
    }
}
