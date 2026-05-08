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
        float minControlHeight = 420f;
        float minWaveformHeight = 120f;
        float minKeyboardHeight = 150f;
        float extraHeight = MathF.Max(0f, availableHeight - (minControlHeight + minWaveformHeight + minKeyboardHeight + (panelGap * 2)));

        float adaptiveControlHeight = MathF.Min(controlPanelHeight, minControlHeight + (extraHeight * 0.30f));
        float adaptiveKeyboardHeight = MathF.Min(keyboardPanelHeight, minKeyboardHeight + (extraHeight * 0.35f));
        float adaptiveWaveformHeight = MathF.Max(minWaveformHeight, availableHeight - adaptiveControlHeight - adaptiveKeyboardHeight - (panelGap * 2));

        Rectangle controlPanel = new(panelMargin, panelMargin, screenWidth - (panelMargin * 2), adaptiveControlHeight);
        Rectangle waveformPanel = new(panelMargin, controlPanel.Y + controlPanel.Height + panelGap, screenWidth - (panelMargin * 2), adaptiveWaveformHeight);
        Rectangle keyboardPanel = new(panelMargin, waveformPanel.Y + waveformPanel.Height + panelGap, screenWidth - (panelMargin * 2), adaptiveKeyboardHeight);

        float sliderRowOneY = controlPanel.Y + 256;
        float sliderRowTwoY = controlPanel.Y + 312;
        float sliderRowThreeY = controlPanel.Y + 368;
        float sliderWidth = (controlPanel.Width - 40 - (18 * 4)) / 5f;
        float filterSliderY = controlPanel.Y + 236;
        float filterSliderRowTwoY = controlPanel.Y + 292;
        float filterSliderRowThreeY = controlPanel.Y + 348;
        float filterSliderWidth = 190f;
        float filterFullWidth = (filterSliderWidth * 2f) + 18f;
        float fxColumnGap = 18f;
        float fxColumnWidth = MathF.Min(380f, (controlPanel.Width - 40f - (fxColumnGap * 2f)) / 3f);
        float fxSliderY = controlPanel.Y + 202;
        float fxSliderRowTwoY = controlPanel.Y + 252;
        float fxSliderWidth = fxColumnWidth;
        float fxFullWidth = fxColumnWidth;
        Rectangle modeButtonsArea = new(controlPanel.X + 20, controlPanel.Y + 66, 420, 36);
        float presetGap = 18f;
        float presetFamilyWidth = MathF.Min(560f, controlPanel.Width * 0.56f);
        float presetOptionWidth = controlPanel.Width - 40f - presetFamilyWidth - presetGap;
        Rectangle presetFamilyArea = new(controlPanel.X + 20, controlPanel.Y + 144, presetFamilyWidth, 212f);
        Rectangle presetOptionArea = new(controlPanel.X + 20 + presetFamilyWidth + presetGap, controlPanel.Y + 144, presetOptionWidth, 212f);
        Rectangle oscillatorButtonsArea = new(controlPanel.X + 20, controlPanel.Y + 118, 360, 36);
        Rectangle waveformButtonsArea = new(controlPanel.X + 20, controlPanel.Y + 184, 500, 56);
        Rectangle filterButtonsArea = new(controlPanel.X + 20, controlPanel.Y + 170, 420, 36);
        Rectangle fxChorusButtonsArea = new(controlPanel.X + 20, controlPanel.Y + 160, fxColumnWidth, 36);
        Rectangle fxReverbButtonsArea = new(controlPanel.X + 20 + fxColumnWidth + fxColumnGap, controlPanel.Y + 160, fxColumnWidth, 36);
        Rectangle fxDelayButtonsArea = new(controlPanel.X + 20 + ((fxColumnWidth + fxColumnGap) * 2f), controlPanel.Y + 160, fxColumnWidth, 36);
        Rectangle filterAnalysisArea = new(controlPanel.X + 20 + filterFullWidth + 38f, controlPanel.Y + 108, controlPanel.Width - ((20 + filterFullWidth + 38f) + 20), controlPanel.Height - 128);

        return new LayoutMetrics(controlPanel, waveformPanel, keyboardPanel, modeButtonsArea, presetFamilyArea, presetOptionArea, oscillatorButtonsArea, filterButtonsArea, fxChorusButtonsArea, fxReverbButtonsArea, fxDelayButtonsArea, filterAnalysisArea, sliderRowOneY, sliderRowTwoY, sliderRowThreeY, sliderWidth, filterSliderY, filterSliderRowTwoY, filterSliderRowThreeY, filterSliderWidth, filterFullWidth, waveformButtonsArea, fxSliderY, fxSliderRowTwoY, fxSliderWidth, fxFullWidth);
    }
}
