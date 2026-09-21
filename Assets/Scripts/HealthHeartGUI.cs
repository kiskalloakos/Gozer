using UnityEngine;

public static class HealthHeartGUI
{
    public static void Draw(int healthUnits, int maxHearts, float x, float y, float spacing = 30f)
    {
        var oldColor = GUI.color;
        var heartStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 19,
            fontStyle = FontStyle.Bold
        };

        for (int i = 0; i < maxHearts; i++)
        {
            var rect = new Rect(x + i * spacing, y, 24f, 26f);
            int unitsInHeart = Mathf.Clamp(healthUnits - i * 2, 0, 2);

            GUI.color = new Color(.2f, .12f, .15f, .9f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            heartStyle.normal.textColor = new Color(.34f, .2f, .23f);
            GUI.Label(new Rect(rect.x, rect.y - 2f, rect.width, rect.height), "♥", heartStyle);

            if (unitsInHeart == 0) continue;

            heartStyle.normal.textColor = Color.white;
            if (unitsInHeart == 2)
            {
                GUI.color = new Color(.92f, .18f, .2f, 1f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(rect.x, rect.y - 2f, rect.width, rect.height), "♥", heartStyle);
                continue;
            }

            GUI.BeginGroup(new Rect(rect.x, rect.y, rect.width * .5f, rect.height));
            GUI.color = new Color(.92f, .18f, .2f, 1f);
            GUI.DrawTexture(new Rect(0f, 0f, rect.width, rect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(0f, -2f, rect.width, rect.height), "♥", heartStyle);
            GUI.EndGroup();
        }

        GUI.color = oldColor;
    }
}
