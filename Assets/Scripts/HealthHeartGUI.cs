using UnityEngine;

public static class HealthHeartGUI
{
    public const float HeartWidth = 24f;
    public const float HeartHeight = 26f;
    public const float DefaultSpacing = 30f;

    const int HeartTextureWidth = 12;
    const int HeartTextureHeight = 10;
    static readonly string[] HeartMask =
    {
        "..##....##..",
        ".####..####.",
        "############",
        "############",
        ".##########.",
        "..########..",
        "...######...",
        "....####....",
        ".....##.....",
        "............"
    };

    static readonly Color FilledColor = new Color(.95f, .08f, .12f, 1f);
    static readonly Color EmptyColor = new Color(.36f, .12f, .15f, .8f);
    static Texture2D fullHeart;
    static Texture2D halfHeart;
    static Texture2D emptyHeart;

    public static float GetWidth(int heartCount, float spacing = DefaultSpacing)
    {
        return heartCount > 0 ? HeartWidth + (heartCount - 1) * spacing : 0f;
    }

    public static void Draw(int healthUnits, int maxHearts, float x, float y,
        float spacing = DefaultSpacing)
    {
        var oldColor = GUI.color;
        GUI.color = Color.white;

        for (int i = 0; i < maxHearts; i++)
        {
            var rect = new Rect(x + i * spacing, y, HeartWidth, HeartHeight);
            int unitsInHeart = Mathf.Clamp(healthUnits - i * 2, 0, 2);
            GUI.DrawTexture(new Rect(rect.x, rect.y + 3f, HeartWidth, 20f),
                GetHeartTexture(unitsInHeart), ScaleMode.StretchToFill, true);
        }

        GUI.color = oldColor;
    }

    static Texture2D GetHeartTexture(int unitsInHeart)
    {
        if (!fullHeart) fullHeart = CreateHeartTexture(2, "Full Heart");
        if (!halfHeart) halfHeart = CreateHeartTexture(1, "Half Heart");
        if (!emptyHeart) emptyHeart = CreateHeartTexture(0, "Empty Heart");
        return unitsInHeart == 2 ? fullHeart : unitsInHeart == 1 ? halfHeart : emptyHeart;
    }

    static Texture2D CreateHeartTexture(int unitsInHeart, string textureName)
    {
        var texture = new Texture2D(HeartTextureWidth, HeartTextureHeight,
            TextureFormat.RGBA32, false)
        {
            name = textureName,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        var pixels = new Color[HeartTextureWidth * HeartTextureHeight];
        for (int y = 0; y < HeartTextureHeight; y++)
        {
            for (int x = 0; x < HeartTextureWidth; x++)
            {
                if (!IsHeartPixel(x, y)) continue;

                bool isOutline = IsOutlinePixel(x, y);
                if (unitsInHeart == 2 || unitsInHeart == 1 && x < HeartTextureWidth / 2)
                    pixels[(HeartTextureHeight - 1 - y) * HeartTextureWidth + x] = FilledColor;
                else if (isOutline)
                    pixels[(HeartTextureHeight - 1 - y) * HeartTextureWidth + x] = EmptyColor;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    static bool IsOutlinePixel(int x, int y)
    {
        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                if (offsetX == 0 && offsetY == 0) continue;
                if (!IsHeartPixel(x + offsetX, y + offsetY)) return true;
            }
        }
        return false;
    }

    static bool IsHeartPixel(int x, int y)
    {
        return x >= 0 && x < HeartTextureWidth && y >= 0 && y < HeartTextureHeight
            && HeartMask[y][x] == '#';
    }
}
