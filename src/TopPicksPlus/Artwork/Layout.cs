namespace TopPicksPlus.Artwork;

/// <summary>
/// The D62/D74 layout in pixels of the 1000x562 picture. The tests do not read these values:
/// they check the drawn picture against D62's own fractions, so a wrong number here fails them.
/// </summary>
internal static class Layout
{
    public const int Width = 1000;
    public const int Height = 562;

    // Left shade: solid black to 22% of the width, fading to clear by 40% (D62).
    public const float ShadeSolidX = 0.22f * Width;
    public const float ShadeClearX = 0.40f * Width;

    // Rank number: left edge 3.5%, top 12%, bottom 90%, inside the 22% band (D62).
    public const float NumberLeft = 0.035f * Width;
    public const float NumberRight = 0.22f * Width;
    public const float NumberTop = 0.12f * Height;
    public const float NumberBottom = 0.90f * Height;

    // Logo / title box: 25%-92% of the width, 35%-94% of the height, bottom-left corner (D62).
    public const float BoxLeft = 0.25f * Width;
    public const float BoxRight = 0.92f * Width;
    public const float BoxTop = 0.35f * Height;
    public const float BoxBottom = 0.94f * Height;

    // No backdrop: poster centred on black between 40% and 100% of the width (D74).
    public const float PosterLeft = 0.40f * Width;

    // Title text (D74: shrinks to fit 2 lines, no smaller than 40 px, then ends in "…"). D97 caps the size.
    public const int TitleMaxChars = 200;        // longer titles are cut first, so fitting stays quick (D97 a)
    public const float TitleMaxSize = 160f;
    public const float TitleMinSize = 40f;
    public const float TitleSizeStep = 2f;
    public const int TitleMaxLines = 2;
    public const float TitleLineSpacing = 1.05f; // baseline to baseline, in ems
    public const float TitleAscent = 1.10f;      // tallest Anton glyph above the baseline (accented capitals), in ems
    public const float TitleDescent = 0.13f;     // deepest Anton glyph below the baseline (g, p, y), in ems

    // Soft dark shadow round the number and the title.
    public const float ShadowOffset = 3f;
    public const float ShadowBlurSigma = 6f;
    public const byte ShadowAlpha = 190;

    // Refuse images bigger than this many pixels (about 200 MB decoded) rather than risk running out of memory.
    public const long MaxSourcePixels = 50_000_000;
}
