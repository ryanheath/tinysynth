using Raylib_CSharp.Rendering;
using Raylib_CSharp.Transformations;
using System.Numerics;
using TinySynth.Synth;
using TinySynth.UI;

namespace TinySynth.App;

internal readonly record struct OscillatorSectionResult(
    int ActiveOscillatorIndex,
    bool ResetActiveSlider);

internal static class OscillatorSectionController
{
    public static OscillatorSectionResult Draw(
        Rectangle controlPanel,
        LayoutMetrics layout,
        int activeOscillatorIndex,
        ref UiControlId? activeSlider,
        float sliderRowOneY,
        float sliderRowTwoY,
        float sliderRowThreeY,
        float sliderWidth,
        Vector2 mousePosition,
        bool mousePressed,
        bool mouseDown,
        SynthParameters synthParameters)
    {
        int previousOscillatorIndex = activeOscillatorIndex;
        activeOscillatorIndex = SynthRenderer.DrawOscillatorButtons(layout.OscillatorButtonsArea, synthParameters.Oscillators, activeOscillatorIndex, mousePosition, mousePressed);

        bool resetActiveSlider = previousOscillatorIndex != activeOscillatorIndex;
        OscillatorParameters activeOscillator = synthParameters.GetOscillator(activeOscillatorIndex);

        Graphics.DrawText($"Waveform · Oscillator {activeOscillatorIndex + 1}", (int)controlPanel.X + 20, (int)controlPanel.Y + 162, 18, UiTheme.MutedTextColor);
        Rectangle oscillatorEnabledBounds = new(layout.OscillatorButtonsArea.X + layout.OscillatorButtonsArea.Width + 20, layout.OscillatorButtonsArea.Y + 6, 192, 24);

        if (mousePressed && UiHitTesting.Contains(oscillatorEnabledBounds, mousePosition))
        {
            activeOscillator.Enabled = !activeOscillator.Enabled;
            resetActiveSlider = true;
        }

        if (resetActiveSlider)
        {
            activeSlider = null;
        }

        SynthRenderer.DrawToggle(
            oscillatorEnabledBounds,
            "Enabled",
            activeOscillator.Enabled);

        activeOscillator.Waveform = SynthRenderer.DrawWaveformButtons(layout.WaveformButtonsArea, activeOscillator.Waveform, activeOscillator.Enabled, mousePosition, mousePressed);
        activeOscillator.EnvelopeMode = SynthRenderer.DrawEnvelopeModeButtons(
            new Rectangle(layout.WaveformButtonsArea.X + layout.WaveformButtonsArea.Width + 18, layout.WaveformButtonsArea.Y, 250, 36),
            activeOscillator.EnvelopeMode,
            activeOscillator.Enabled,
            mousePosition,
            mousePressed);

        activeOscillator.Gain = SynthRenderer.DrawSlider(
            index: UiControlId.OscillatorGain,
            activeSlider: ref activeSlider,
            enabled: activeOscillator.Enabled,
            label: "Gain",
            valueLabel: $"{activeOscillator.Gain:0.00}",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 0), sliderRowOneY, sliderWidth, 20),
            value: activeOscillator.Gain,
            minValue: 0.00f,
            maxValue: 1.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        activeOscillator.DetuneCents = SynthRenderer.DrawSlider(
            index: UiControlId.OscillatorDetune,
            activeSlider: ref activeSlider,
            enabled: activeOscillator.Enabled,
            label: "Detune",
            valueLabel: $"{activeOscillator.DetuneCents:0} ct",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 1), sliderRowOneY, sliderWidth, 20),
            value: activeOscillator.DetuneCents,
            minValue: -100.00f,
            maxValue: 100.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        activeOscillator.GlideSeconds = SynthRenderer.DrawSlider(
            index: UiControlId.OscillatorGlide,
            activeSlider: ref activeSlider,
            enabled: activeOscillator.Enabled,
            label: "Glide",
            valueLabel: $"{activeOscillator.GlideSeconds:0.00}s",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 2), sliderRowOneY, sliderWidth, 20),
            value: activeOscillator.GlideSeconds,
            minValue: 0.00f,
            maxValue: 1.50f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        activeOscillator.VibratoDepthCents = SynthRenderer.DrawSlider(
            index: UiControlId.OscillatorVibratoDepth,
            activeSlider: ref activeSlider,
            enabled: activeOscillator.Enabled,
            label: "Vib depth",
            valueLabel: $"{activeOscillator.VibratoDepthCents:0} ct",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 3), sliderRowOneY, sliderWidth, 20),
            value: activeOscillator.VibratoDepthCents,
            minValue: 0.00f,
            maxValue: 100.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        activeOscillator.VibratoRateHz = SynthRenderer.DrawSlider(
            index: UiControlId.OscillatorVibratoRate,
            activeSlider: ref activeSlider,
            enabled: activeOscillator.Enabled,
            label: "Vib rate",
            valueLabel: $"{activeOscillator.VibratoRateHz:0.0}Hz",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 4), sliderRowOneY, sliderWidth, 20),
            value: activeOscillator.VibratoRateHz,
            minValue: 0.10f,
            maxValue: 12.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        activeOscillator.AttackSeconds = SynthRenderer.DrawSlider(
            index: UiControlId.OscillatorAttack,
            activeSlider: ref activeSlider,
            enabled: activeOscillator.Enabled,
            label: "Attack",
            valueLabel: $"{activeOscillator.AttackSeconds:0.00}s",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 0), sliderRowTwoY, sliderWidth, 20),
            value: activeOscillator.AttackSeconds,
            minValue: 0.01f,
            maxValue: 2.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        activeOscillator.DecaySeconds = SynthRenderer.DrawSlider(
            index: UiControlId.OscillatorDecay,
            activeSlider: ref activeSlider,
            enabled: activeOscillator.Enabled,
            label: "Decay",
            valueLabel: $"{activeOscillator.DecaySeconds:0.00}s",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 1), sliderRowTwoY, sliderWidth, 20),
            value: activeOscillator.DecaySeconds,
            minValue: 0.01f,
            maxValue: 2.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        activeOscillator.SustainLevel = SynthRenderer.DrawSlider(
            index: UiControlId.OscillatorSustain,
            activeSlider: ref activeSlider,
            enabled: activeOscillator.Enabled,
            label: "Sustain",
            valueLabel: $"{activeOscillator.SustainLevel:0.00}",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 2), sliderRowTwoY, sliderWidth, 20),
            value: activeOscillator.SustainLevel,
            minValue: 0.00f,
            maxValue: 1.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        activeOscillator.ReleaseSeconds = SynthRenderer.DrawSlider(
            index: UiControlId.OscillatorRelease,
            activeSlider: ref activeSlider,
            enabled: activeOscillator.Enabled,
            label: "Release",
            valueLabel: $"{activeOscillator.ReleaseSeconds:0.00}s",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 3), sliderRowTwoY, sliderWidth, 20),
            value: activeOscillator.ReleaseSeconds,
            minValue: 0.01f,
            maxValue: 2.50f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        activeOscillator.PulseWidth = SynthRenderer.DrawSlider(
            index: UiControlId.OscillatorPulseWidth,
            activeSlider: ref activeSlider,
            enabled: activeOscillator.Enabled && activeOscillator.Waveform == Waveform.Square,
            label: "Pulse width",
            valueLabel: $"{(activeOscillator.PulseWidth * 100f):0}%",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 0), sliderRowThreeY, sliderWidth, 20),
            value: activeOscillator.PulseWidth,
            minValue: 0.10f,
            maxValue: 0.90f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        activeOscillator.PwmRateHz = SynthRenderer.DrawSlider(
            index: UiControlId.OscillatorPwmRate,
            activeSlider: ref activeSlider,
            enabled: activeOscillator.Enabled && activeOscillator.Waveform == Waveform.Square,
            label: "PWM rate",
            valueLabel: $"{activeOscillator.PwmRateHz:0.0}Hz",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 1), sliderRowThreeY, sliderWidth, 20),
            value: activeOscillator.PwmRateHz,
            minValue: 0.00f,
            maxValue: 12.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        activeOscillator.Pan = SynthRenderer.DrawSlider(
            index: UiControlId.OscillatorPan,
            activeSlider: ref activeSlider,
            enabled: activeOscillator.Enabled,
            label: "Pan",
            valueLabel: $"{activeOscillator.Pan:0.00}",
            bounds: new Rectangle(controlPanel.X + 20 + ((sliderWidth + 18) * 2), sliderRowThreeY, sliderWidth, 20),
            value: activeOscillator.Pan,
            minValue: -1.00f,
            maxValue: 1.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        return new OscillatorSectionResult(activeOscillatorIndex, resetActiveSlider);
    }
}
