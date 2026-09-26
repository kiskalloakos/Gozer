using UnityEngine;

public static class EnergyBoltGUI
{
    public const float BoltWidth = 16f;
    public const float BoltHeight = 24f;
    public const float DefaultSpacing = 21f;

    static readonly string[] BoltMask =
    {
        "...##...",
        "..####..",
        ".######.",
        "..#####.",
        "...####.",
        "....###.",
        "...###..",
        "..###...",
        ".###....",
        "##......",
        "#.......",
        "........"
    };

    static readonly Color FilledColor = new Color(1f, .84f, .18f, 1f);
    static readonly Color EmptyColor = new Color(.28f, .23f, .08f, .85f);

    public static float GetWidth(int boltCount, float spacing = DefaultSpacing)
        => boltCount > 0 ? BoltWidth + (boltCount - 1) * spacing : 0f;

    public static void Draw(float energy, int maxBolts, float x, float y,
        float spacing = DefaultSpacing)
    {
        var oldColor = GUI.color;
        float pixel = BoltWidth / 8f;

        for (int boltIndex = 0; boltIndex < maxBolts; boltIndex++)
        {
            float fill = Mathf.Clamp01(energy - boltIndex);
            for (int row = 0; row < BoltMask.Length; row++)
            {
                string maskRow = BoltMask[row];
                for (int column = 0; column < maskRow.Length; column++)
                {
                    if (maskRow[column] != '#') continue;
                    GUI.color = HouseSceneFade.FadeColor(column + .5f <= maskRow.Length * fill
                        ? FilledColor
                        : EmptyColor);
                    GUI.DrawTexture(new Rect(
                        Mathf.Round(x + boltIndex * spacing + column * pixel),
                        Mathf.Round(y + row * (BoltHeight / BoltMask.Length)),
                        Mathf.Ceil(pixel),
                        Mathf.Ceil(BoltHeight / BoltMask.Length)),
                        Texture2D.whiteTexture);
                }
            }
        }

        GUI.color = oldColor;
    }
}
