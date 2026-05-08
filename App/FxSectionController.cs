using Raylib_CSharp.Rendering;
using Raylib_CSharp.Transformations;
using System.Numerics;
using TinySynth.Synth;
using TinySynth.UI;

namespace TinySynth.App;

internal static class FxSectionController
{
    public static void Draw(
        Rectangle controlPanel,
        LayoutMetrics layout,
        ref int activeSlider,
        float fxSliderY,
        float fxSliderRowTwoY,
        float fxSliderWidth,
        Vector2 mousePosition,
        bool mousePressed,
        bool mouseDown,
        SynthParameters synthParameters)
    {
        float fxMiddleColumnX = layout.FxReverbButtonsArea.X;
        float fxRightColumnX = layout.FxDelayButtonsArea.X;

        Graphics.DrawText("Chorus type", (int)controlPanel.X + 20, (int)controlPanel.Y + 138, 18, UiTheme.MutedTextColor);
        synthParameters.ChorusType = SynthRenderer.DrawChorusButtons(layout.FxChorusButtonsArea, synthParameters.ChorusType, mousePosition, mousePressed);

        Graphics.DrawText("Reverb type", (int)fxMiddleColumnX, (int)controlPanel.Y + 138, 18, UiTheme.MutedTextColor);
        synthParameters.ReverbType = SynthRenderer.DrawReverbButtons(layout.FxReverbButtonsArea, synthParameters.ReverbType, mousePosition, mousePressed);

        Graphics.DrawText("Delay type", (int)fxRightColumnX, (int)controlPanel.Y + 138, 18, UiTheme.MutedTextColor);
        synthParameters.DelayType = SynthRenderer.DrawDelayButtons(layout.FxDelayButtonsArea, synthParameters.DelayType, mousePosition, mousePressed);

        bool chorusEnabled = synthParameters.ChorusType != ChorusType.Off;
        bool reverbEnabled = synthParameters.ReverbType != ReverbType.Off;
        bool delayEnabled = synthParameters.DelayType != DelayType.Off;

        synthParameters.ChorusMix = SynthRenderer.DrawSlider(
            index: 15,
            activeSlider: ref activeSlider,
            enabled: chorusEnabled,
            label: "Chorus mix",
            valueLabel: $"{(synthParameters.ChorusMix * 100f):0}%",
            bounds: new Rectangle(controlPanel.X + 20, fxSliderY, fxSliderWidth, 20),
            value: synthParameters.ChorusMix,
            minValue: 0.00f,
            maxValue: 1.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        synthParameters.ChorusRateHz = SynthRenderer.DrawSlider(
            index: 16,
            activeSlider: ref activeSlider,
            enabled: chorusEnabled,
            label: "Chorus rate",
            valueLabel: $"{synthParameters.ChorusRateHz:0.0}Hz",
            bounds: new Rectangle(controlPanel.X + 20, fxSliderRowTwoY, fxSliderWidth, 20),
            value: synthParameters.ChorusRateHz,
            minValue: 0.10f,
            maxValue: 3.50f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        synthParameters.ChorusDepth = SynthRenderer.DrawSlider(
            index: 17,
            activeSlider: ref activeSlider,
            enabled: chorusEnabled,
            label: "Chorus depth",
            valueLabel: $"{synthParameters.ChorusDepth:0.00}",
            bounds: new Rectangle(controlPanel.X + 20, fxSliderRowTwoY + 56, fxSliderWidth, 20),
            value: synthParameters.ChorusDepth,
            minValue: 0.05f,
            maxValue: 1.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        synthParameters.ChorusTremoloDepth = SynthRenderer.DrawSlider(
            index: 27,
            activeSlider: ref activeSlider,
            enabled: chorusEnabled,
            label: "Chorus trem",
            valueLabel: $"{(synthParameters.ChorusTremoloDepth * 100f):0}%",
            bounds: new Rectangle(controlPanel.X + 20, fxSliderRowTwoY + 112, fxSliderWidth, 20),
            value: synthParameters.ChorusTremoloDepth,
            minValue: 0.00f,
            maxValue: 1.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        synthParameters.ReverbMix = SynthRenderer.DrawSlider(
            index: 18,
            activeSlider: ref activeSlider,
            enabled: reverbEnabled,
            label: "Reverb mix",
            valueLabel: $"{(synthParameters.ReverbMix * 100f):0}%",
            bounds: new Rectangle(fxMiddleColumnX, fxSliderY, fxSliderWidth, 20),
            value: synthParameters.ReverbMix,
            minValue: 0.00f,
            maxValue: 1.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        synthParameters.ReverbSize = SynthRenderer.DrawSlider(
            index: 19,
            activeSlider: ref activeSlider,
            enabled: reverbEnabled,
            label: "Reverb size",
            valueLabel: $"{synthParameters.ReverbSize:0.00}",
            bounds: new Rectangle(fxMiddleColumnX, fxSliderRowTwoY, fxSliderWidth, 20),
            value: synthParameters.ReverbSize,
            minValue: 0.10f,
            maxValue: 1.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        synthParameters.ReverbDamping = SynthRenderer.DrawSlider(
            index: 20,
            activeSlider: ref activeSlider,
            enabled: reverbEnabled,
            label: "Reverb damp",
            valueLabel: $"{synthParameters.ReverbDamping:0.00}",
            bounds: new Rectangle(fxMiddleColumnX, fxSliderRowTwoY + 56, fxSliderWidth, 20),
            value: synthParameters.ReverbDamping,
            minValue: 0.00f,
            maxValue: 1.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        synthParameters.DelayMix = SynthRenderer.DrawSlider(
            index: 21,
            activeSlider: ref activeSlider,
            enabled: delayEnabled,
            label: "Delay mix",
            valueLabel: $"{(synthParameters.DelayMix * 100f):0}%",
            bounds: new Rectangle(fxRightColumnX, fxSliderY, fxSliderWidth, 20),
            value: synthParameters.DelayMix,
            minValue: 0.00f,
            maxValue: 1.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        synthParameters.DelayTimeSeconds = SynthRenderer.DrawSlider(
            index: 22,
            activeSlider: ref activeSlider,
            enabled: delayEnabled,
            label: "Delay time",
            valueLabel: $"{synthParameters.DelayTimeSeconds:0.00}s",
            bounds: new Rectangle(fxRightColumnX, fxSliderRowTwoY, fxSliderWidth, 20),
            value: synthParameters.DelayTimeSeconds,
            minValue: 0.05f,
            maxValue: 0.90f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        synthParameters.DelayFeedback = SynthRenderer.DrawSlider(
            index: 23,
            activeSlider: ref activeSlider,
            enabled: delayEnabled,
            label: "Delay fb",
            valueLabel: $"{synthParameters.DelayFeedback:0.00}",
            bounds: new Rectangle(fxRightColumnX, fxSliderRowTwoY + 56, fxSliderWidth, 20),
            value: synthParameters.DelayFeedback,
            minValue: 0.00f,
            maxValue: 0.85f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);
    }
}
