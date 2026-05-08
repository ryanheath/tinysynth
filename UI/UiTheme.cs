using Raylib_CSharp.Colors;

namespace TinySynth.UI;

internal static class UiTheme
{
    public static Color BackgroundColor { get; } = new(242, 245, 250, 255);
    public static Color PanelColor { get; } = new(252, 253, 255, 255);
    public static Color KeyboardPanelColor { get; } = new(233, 238, 247, 255);
    public static Color PanelHoverColor { get; } = new(245, 248, 255, 255);
    public static Color BorderColor { get; } = new(208, 214, 224, 255);
    public static Color TextColor { get; } = new(52, 60, 76, 255);
    public static Color MutedTextColor { get; } = new(105, 114, 132, 255);
    public static Color DisabledFillColor { get; } = new(236, 239, 245, 255);
    public static Color DisabledPanelColor { get; } = new(243, 245, 249, 255);
    public static Color DisabledBorderColor { get; } = new(194, 201, 214, 255);
    public static Color DisabledTextColor { get; } = new(134, 143, 160, 255);
    public static Color DisabledMutedTextColor { get; } = new(160, 168, 183, 255);
    public static Color DisabledSecondaryTextColor { get; } = new(160, 168, 183, 255);
    public static Color AccentColor { get; } = new(84, 146, 255, 255);
    public static Color AccentStrongColor { get; } = new(47, 111, 237, 255);
    public static Color AccentSoftColor { get; } = new(213, 231, 255, 255);
    public static Color DisabledAccentColor { get; } = new(182, 189, 201, 255);
    public static Color DisabledAccentSoftColor { get; } = new(224, 228, 236, 255);
    public static Color WhiteKeyColor { get; } = new(255, 255, 255, 255);
    public static Color WhiteKeyHoverColor { get; } = new(242, 247, 255, 255);
    public static Color DarkKeyColor { get; } = new(40, 46, 60, 255);
    public static Color DarkKeyHoverColor { get; } = new(70, 78, 99, 255);
    public static Color DarkKeyBorderColor { get; } = new(18, 22, 30, 255);
    public static Color AnalysisSurfaceColor { get; } = new(246, 249, 255, 255);
    public static Color AnalysisGridColor { get; } = new(225, 231, 242, 255);
    public static Color ScopeCenterLineColor { get; } = new(215, 223, 237, 255);
    public static Color PreviewGuideColor { get; } = new(208, 214, 224, 90);
}
