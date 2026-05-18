using Raylib_CSharp.Rendering;
using Raylib_CSharp.Transformations;
using System.Numerics;
using TinySynth.App.Services;
using TinySynth.Synth;
using TinySynth.UI;

namespace TinySynth.App;

internal static class FilterSectionController
{
    public static void Draw(
        Rectangle controlPanel,
        LayoutMetrics layout,
        AudioStreamPump audioStreamPump,
        int sampleRate,
        ref UiControlId? activeSlider,
        Vector2 mousePosition,
        bool mousePressed,
        bool mouseDown,
        SynthParameters synthParameters)
    {
        float maxFilterCutoffHz = MathF.Max(20f, sampleRate * 0.45f);
        float normalizedCutoff = GetLogNormalizedFrequency(synthParameters.FilterCutoffHz, 20f, maxFilterCutoffHz);

        Graphics.DrawText("Filter routing", (int)controlPanel.X + 20, (int)controlPanel.Y + 138, 18, UiTheme.MutedTextColor);
        synthParameters.FilterType = SynthRenderer.DrawFilterButtons(layout.FilterButtonsArea, synthParameters.FilterType, mousePosition, mousePressed);

        normalizedCutoff = SynthRenderer.DrawSlider(
            index: UiControlId.FilterCutoff,
            activeSlider: ref activeSlider,
            enabled: synthParameters.FilterType != FilterType.Off,
            label: "Cutoff",
            valueLabel: $"{synthParameters.FilterCutoffHz:0} Hz",
            bounds: new Rectangle(controlPanel.X + 20, layout.FilterSliderY, layout.FilterSliderWidth, 20),
            value: normalizedCutoff,
            minValue: 0f,
            maxValue: 1f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        synthParameters.FilterCutoffHz = GetFrequencyFromLogNormalized(normalizedCutoff, 20f, maxFilterCutoffHz);

        synthParameters.FilterResonance = SynthRenderer.DrawSlider(
            index: UiControlId.FilterResonance,
            activeSlider: ref activeSlider,
            enabled: synthParameters.FilterType != FilterType.Off,
            label: "Resonance",
            valueLabel: $"{synthParameters.FilterResonance:0.00}",
            bounds: new Rectangle(controlPanel.X + 20 + layout.FilterSliderWidth + 18, layout.FilterSliderY, layout.FilterSliderWidth, 20),
            value: synthParameters.FilterResonance,
            minValue: 0.00f,
            maxValue: 1.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        synthParameters.FilterEnvelopeAmount = SynthRenderer.DrawSlider(
            index: UiControlId.FilterEnvelopeAmount,
            activeSlider: ref activeSlider,
            enabled: synthParameters.FilterType != FilterType.Off,
            label: "Env amt",
            valueLabel: $"{synthParameters.FilterEnvelopeAmount:0.00}x",
            bounds: new Rectangle(controlPanel.X + 20, layout.FilterSliderRowTwoY, layout.FilterSliderWidth, 20),
            value: synthParameters.FilterEnvelopeAmount,
            minValue: -2.00f,
            maxValue: 2.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        synthParameters.FilterLfoDepth = SynthRenderer.DrawSlider(
            index: UiControlId.FilterLfoDepth,
            activeSlider: ref activeSlider,
            enabled: synthParameters.FilterType != FilterType.Off,
            label: "LFO depth",
            valueLabel: $"{synthParameters.FilterLfoDepth:0.00}x",
            bounds: new Rectangle(controlPanel.X + 20 + layout.FilterSliderWidth + 18, layout.FilterSliderRowTwoY, layout.FilterSliderWidth, 20),
            value: synthParameters.FilterLfoDepth,
            minValue: 0.00f,
            maxValue: 2.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        synthParameters.FilterLfoRateHz = SynthRenderer.DrawSlider(
            index: UiControlId.FilterLfoRate,
            activeSlider: ref activeSlider,
            enabled: synthParameters.FilterType != FilterType.Off,
            label: "LFO rate",
            valueLabel: $"{synthParameters.FilterLfoRateHz:0.0}Hz",
            bounds: new Rectangle(controlPanel.X + 20, layout.FilterSliderRowThreeY, layout.FilterFullWidth, 20),
            value: synthParameters.FilterLfoRateHz,
            minValue: 0.10f,
            maxValue: 12.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        SynthRenderer.DrawFilterAnalysis(
            layout.FilterAnalysisArea,
            audioStreamPump.ScopeBuffer,
            audioStreamPump.ScopeWriteIndex,
            sampleRate: sampleRate,
            filterType: synthParameters.FilterType,
            cutoffHz: synthParameters.FilterCutoffHz,
            resonance: synthParameters.FilterResonance);
    }

    private static float GetLogNormalizedFrequency(float frequency, float minFrequency, float maxFrequency)
    {
        minFrequency = MathF.Max(minFrequency, 0.0001f);
        maxFrequency = MathF.Max(maxFrequency, minFrequency);
        frequency = Math.Clamp(frequency, minFrequency, maxFrequency);

        float minLog = MathF.Log(minFrequency);
        float maxLog = MathF.Log(maxFrequency);
        return (MathF.Log(frequency) - minLog) / (maxLog - minLog);
    }

    private static float GetFrequencyFromLogNormalized(float normalizedValue, float minFrequency, float maxFrequency)
    {
        minFrequency = MathF.Max(minFrequency, 0.0001f);
        maxFrequency = MathF.Max(maxFrequency, minFrequency);
        normalizedValue = Math.Clamp(normalizedValue, 0f, 1f);

        float minLog = MathF.Log(minFrequency);
        float maxLog = MathF.Log(maxFrequency);
        return MathF.Exp(minLog + ((maxLog - minLog) * normalizedValue));
    }
}
