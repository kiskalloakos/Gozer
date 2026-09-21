using UnityEngine;

[CreateAssetMenu(fileName = "GoldVisualAssets", menuName = "RPG/Gold Visual Assets")]
public sealed class GoldVisualAssets : ScriptableObject
{
    const string ResourcePath = "GoldVisualAssets";

    static GoldVisualAssets instance;

    public Sprite[] floatingFrames;
    public Sprite inventorySprite;
    [Min(1f)] public float framesPerSecond = PixelArtStandard.WalkFramesPerSecond;

    public static GoldVisualAssets Load()
    {
        if (!instance) instance = Resources.Load<GoldVisualAssets>(ResourcePath);
        return instance;
    }
}
