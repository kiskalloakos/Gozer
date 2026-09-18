/// <summary>
/// Canonical numerical settings for Gozer's 16-pixel orthographic art pipeline.
/// Keep these values synchronized with PIXEL_ART_STANDARD.md.
/// </summary>
public static class PixelArtStandard
{
    public const int PixelsPerUnit = 16;
    public const int TilePixels = 16;
    public const int ReferenceWidth = 320;
    public const int ReferenceHeight = 180;
    public const float OrthographicSize = ReferenceHeight / (PixelsPerUnit * 2f);
    public const float WalkFramesPerSecond = 8f;
}
