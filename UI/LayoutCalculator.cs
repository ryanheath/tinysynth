using Raylib_CSharp.Transformations;

namespace TinySynth.UI;

internal static class LayoutCalculator
{
    public static LayoutMetrics Calculate(
        int screenWidth,
        int screenHeight,
        float panelMargin,
        float panelGap,
        float controlPanelHeight,
        float keyboardPanelHeight)
    {
        float availableHeight = screenHeight - (panelMargin * 2);
        float minControlHeight = 300f;
        float minWaveformHeight = 120f;
        float minKeyboardHeight = 150f;
        float extraHeight = MathF.Max(0f, availableHeight - (minControlHeight + minWaveformHeight + minKeyboardHeight + (panelGap * 2)));

        float adaptiveControlHeight = MathF.Min(controlPanelHeight, minControlHeight + (extraHeight * 0.30f));
        float adaptiveKeyboardHeight = MathF.Min(keyboardPanelHeight, minKeyboardHeight + (extraHeight * 0.35f));
        float adaptiveWaveformHeight = MathF.Max(minWaveformHeight, availableHeight - adaptiveControlHeight - adaptiveKeyboardHeight - (panelGap * 2));

        Rectangle controlPanel = new(panelMargin, panelMargin, screenWidth - (panelMargin * 2), adaptiveControlHeight);
        Rectangle waveformPanel = new(panelMargin, controlPanel.Y + controlPanel.Height + panelGap, screenWidth - (panelMargin * 2), adaptiveWaveformHeight);
        Rectangle keyboardPanel = new(panelMargin, waveformPanel.Y + waveformPanel.Height + panelGap, screenWidth - (panelMargin * 2), adaptiveKeyboardHeight);

        float sliderRowOneY = controlPanel.Y + 188;
        float sliderRowTwoY = controlPanel.Y + 244;
        float sliderWidth = (controlPanel.Width - 40 - (18 * 4)) / 5f;
        Rectangle oscillatorButtonsArea = new(controlPanel.X + 20, controlPanel.Y + 66, 360, 36);
        Rectangle waveformButtonsArea = new(controlPanel.X + 20, controlPanel.Y + 134, 360, 36);

        return new LayoutMetrics(controlPanel, waveformPanel, keyboardPanel, oscillatorButtonsArea, sliderRowOneY, sliderRowTwoY, sliderWidth, waveformButtonsArea);
    }
}
