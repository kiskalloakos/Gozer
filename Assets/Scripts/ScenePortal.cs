using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePortal : MonoBehaviour
{
    public string destinationScene;
    public string prompt = "[E] Travel";
    public float activationRadius = 2.2f;

    private Transform player;
    private bool playerNearby;

    void Update()
    {
        if (!player) FindPlayer();
        playerNearby = player && Vector2.Distance(player.position, transform.position) <= activationRadius;

        if (playerNearby && Input.GetKeyDown(KeyCode.E))
            SceneManager.LoadScene(destinationScene);
    }

    private void FindPlayer()
    {
        var townPlayer = FindAnyObjectByType<TownPlayerController>();
        if (townPlayer) { player = townPlayer.transform; return; }

        var rogue = FindAnyObjectByType<RogueController>();
        if (rogue) player = rogue.transform;
    }

    void OnGUI()
    {
        if (!playerNearby) return;

        var style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };
        var oldColor = GUI.color;
        GUI.color = new Color(.08f, .055f, .04f, .94f);
        GUI.Box(new Rect(Screen.width / 2f - 245, Screen.height - 82, 490, 48), GUIContent.none);
        GUI.color = new Color(1f, .88f, .52f);
        GUI.Label(new Rect(Screen.width / 2f - 235, Screen.height - 76, 470, 36), prompt, style);
        GUI.color = oldColor;
    }
}
