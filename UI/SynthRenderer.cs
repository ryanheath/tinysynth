using Raylib_CSharp.Colors;
using Raylib_CSharp.Fonts;
using Raylib_CSharp.Rendering;
using Raylib_CSharp.Transformations;
using System.Numerics;
using TinySynth.Synth;

namespace TinySynth.UI;

internal static class SynthRenderer
{
    public static int DrawOscillatorButtons(
        Rectangle area,
        IReadOnlyList<OscillatorParameters> oscillators,
        int activeOscillatorIndex,
        Vector2 mousePosition,
        bool mousePressed,
        Color panelColor,
        Color borderColor,
        Color selectedColor,
        Color selectedBorderColor,
        Color textColor)
    {
        float buttonGap = 10f;
        float buttonWidth = (area.Width - (buttonGap * (SynthParameters.OscillatorCount - 1))) / SynthParameters.OscillatorCount;

        for (int i = 0; i < SynthParameters.OscillatorCount; i++)
        {
            Rectangle buttonBounds = new(area.X + (i * (buttonWidth + buttonGap)), area.Y, buttonWidth, area.Height);
            bool isEnabled = oscillators[i].Enabled;
            bool isSelected = activeOscillatorIndex == i;
            bool isHovered = Contains(buttonBounds, mousePosition);
            Color fill = isEnabled
                ? (isSelected ? selectedColor : (isHovered ? new Color(245, 248, 255, 255) : panelColor))
                : new Color(236, 239, 245, 255);
            Color outline = isEnabled
                ? (isSelected ? selectedBorderColor : borderColor)
                : new Color(194, 201, 214, 255);
            Color buttonTextColor = isEnabled ? textColor : new Color(134, 143, 160, 255);

            Graphics.DrawRectangleRec(buttonBounds, fill);
            Graphics.DrawRectangleLinesEx(buttonBounds, isSelected ? 2.5f : 1f, outline);
            Graphics.DrawText($"Osc {i + 1}", (int)buttonBounds.X + 14, (int)buttonBounds.Y + 7, 18, buttonTextColor);
            Graphics.DrawText(isEnabled ? "On" : "Off", (int)buttonBounds.X + 20, (int)buttonBounds.Y + 21, 12, buttonTextColor);

            if (mousePressed && isHovered)
            {
                activeOscillatorIndex = i;
            }
        }

        return activeOscillatorIndex;
    }

    public static Waveform DrawWaveformButtons(
        Rectangle area,
        Waveform currentValue,
        bool enabled,
        Vector2 mousePosition,
        bool mousePressed,
        Color panelColor,
        Color borderColor,
        Color selectedColor,
        Color selectedBorderColor,
        Color textColor)
    {
        Waveform[] waveforms = [Waveform.Sine, Waveform.Square, Waveform.Saw, Waveform.Triangle, Waveform.Noise];
        float buttonGap = 10f;
        float buttonWidth = (area.Width - (buttonGap * (waveforms.Length - 1))) / waveforms.Length;

        for (int i = 0; i < waveforms.Length; i++)
        {
            Rectangle buttonBounds = new(area.X + (i * (buttonWidth + buttonGap)), area.Y, buttonWidth, area.Height);
            bool isSelected = currentValue == waveforms[i];
            bool isHovered = Contains(buttonBounds, mousePosition);
            string buttonLabel = waveforms[i].ToString();
            const int buttonFontSize = 18;
            Color fill = enabled
                ? (isSelected ? selectedColor : (isHovered ? new Color(245, 248, 255, 255) : panelColor))
                : new Color(236, 239, 245, 255);
            Color outline = enabled
                ? (isSelected ? selectedBorderColor : borderColor)
                : new Color(194, 201, 214, 255);
            Color buttonTextColor = enabled ? textColor : new Color(134, 143, 160, 255);

            Graphics.DrawRectangleRec(buttonBounds, fill);
            Graphics.DrawRectangleLinesEx(buttonBounds, isSelected ? 2.5f : 1f, outline);
            int textWidth = TextManager.MeasureText(buttonLabel, buttonFontSize);
            int textX = (int)(buttonBounds.X + ((buttonBounds.Width - textWidth) / 2f));
            int textY = (int)(buttonBounds.Y + ((buttonBounds.Height - buttonFontSize) / 2f) - 1f);
            Graphics.DrawText(buttonLabel, textX, textY, buttonFontSize, buttonTextColor);

            if (enabled && mousePressed && isHovered)
            {
                currentValue = waveforms[i];
            }
        }

        return currentValue;
    }

    public static int DrawParameterSectionButtons(
        Rectangle area,
        int selectedIndex,
        Vector2 mousePosition,
        bool mousePressed,
        Color panelColor,
        Color borderColor,
        Color selectedColor,
        Color selectedBorderColor,
        Color textColor)
    {
        string[] labels = ["Presets", "Oscillator", "Filter", "FX"];
        float buttonGap = 10f;
        float buttonWidth = (area.Width - (buttonGap * (labels.Length - 1))) / labels.Length;

        for (int i = 0; i < labels.Length; i++)
        {
            Rectangle buttonBounds = new(area.X + (i * (buttonWidth + buttonGap)), area.Y, buttonWidth, area.Height);
            bool isSelected = selectedIndex == i;
            bool isHovered = Contains(buttonBounds, mousePosition);
            const int buttonFontSize = 18;
            Color fill = isSelected ? selectedColor : (isHovered ? new Color(245, 248, 255, 255) : panelColor);
            Color outline = isSelected ? selectedBorderColor : borderColor;

            Graphics.DrawRectangleRec(buttonBounds, fill);
            Graphics.DrawRectangleLinesEx(buttonBounds, isSelected ? 2.5f : 1f, outline);
            int textWidth = TextManager.MeasureText(labels[i], buttonFontSize);
            int textX = (int)(buttonBounds.X + ((buttonBounds.Width - textWidth) / 2f));
            int textY = (int)(buttonBounds.Y + ((buttonBounds.Height - buttonFontSize) / 2f) - 1f);
            Graphics.DrawText(labels[i], textX, textY, buttonFontSize, textColor);

            if (mousePressed && isHovered)
            {
                selectedIndex = i;
            }
        }

        return selectedIndex;
    }

    public static FilterType DrawFilterButtons(
        Rectangle area,
        FilterType currentValue,
        Vector2 mousePosition,
        bool mousePressed,
        Color panelColor,
        Color borderColor,
        Color selectedColor,
        Color selectedBorderColor,
        Color textColor)
    {
        FilterType[] filterTypes = [FilterType.Off, FilterType.LowPass, FilterType.HighPass, FilterType.BandPass];
        float buttonGap = 10f;
        float buttonWidth = (area.Width - (buttonGap * (filterTypes.Length - 1))) / filterTypes.Length;

        for (int i = 0; i < filterTypes.Length; i++)
        {
            Rectangle buttonBounds = new(area.X + (i * (buttonWidth + buttonGap)), area.Y, buttonWidth, area.Height);
            bool isSelected = currentValue == filterTypes[i];
            bool isHovered = Contains(buttonBounds, mousePosition);
            string buttonLabel = filterTypes[i] switch
            {
                FilterType.Off => "Off",
                FilterType.LowPass => "Low-pass",
                FilterType.HighPass => "High-pass",
                FilterType.BandPass => "Band-pass",
                _ => filterTypes[i].ToString()
            };
            const int buttonFontSize = 18;
            Color fill = isSelected ? selectedColor : (isHovered ? new Color(245, 248, 255, 255) : panelColor);
            Color outline = isSelected ? selectedBorderColor : borderColor;

            Graphics.DrawRectangleRec(buttonBounds, fill);
            Graphics.DrawRectangleLinesEx(buttonBounds, isSelected ? 2.5f : 1f, outline);
            int textWidth = TextManager.MeasureText(buttonLabel, buttonFontSize);
            int textX = (int)(buttonBounds.X + ((buttonBounds.Width - textWidth) / 2f));
            int textY = (int)(buttonBounds.Y + ((buttonBounds.Height - buttonFontSize) / 2f) - 1f);
            Graphics.DrawText(buttonLabel, textX, textY, buttonFontSize, textColor);

            if (mousePressed && isHovered)
            {
                currentValue = filterTypes[i];
            }
        }

        return currentValue;
    }

    public static ChorusType DrawChorusButtons(
        Rectangle area,
        ChorusType currentValue,
        Vector2 mousePosition,
        bool mousePressed,
        Color panelColor,
        Color borderColor,
        Color selectedColor,
        Color selectedBorderColor,
        Color textColor)
    {
        ChorusType[] chorusTypes = [ChorusType.Off, ChorusType.Light, ChorusType.Ensemble, ChorusType.Wide];
        return DrawEnumButtons(area, chorusTypes, currentValue, mousePosition, mousePressed, panelColor, borderColor, selectedColor, selectedBorderColor, textColor, static value => value.ToString());
    }

    public static ReverbType DrawReverbButtons(
        Rectangle area,
        ReverbType currentValue,
        Vector2 mousePosition,
        bool mousePressed,
        Color panelColor,
        Color borderColor,
        Color selectedColor,
        Color selectedBorderColor,
        Color textColor)
    {
        ReverbType[] reverbTypes = [ReverbType.Off, ReverbType.Room, ReverbType.Hall, ReverbType.Shimmer];
        return DrawEnumButtons(area, reverbTypes, currentValue, mousePosition, mousePressed, panelColor, borderColor, selectedColor, selectedBorderColor, textColor, static value => value.ToString());
    }

    public static DelayType DrawDelayButtons(
        Rectangle area,
        DelayType currentValue,
        Vector2 mousePosition,
        bool mousePressed,
        Color panelColor,
        Color borderColor,
        Color selectedColor,
        Color selectedBorderColor,
        Color textColor)
    {
        DelayType[] delayTypes = [DelayType.Off, DelayType.Slap, DelayType.PingPong, DelayType.Tape];
        return DrawEnumButtons(area, delayTypes, currentValue, mousePosition, mousePressed, panelColor, borderColor, selectedColor, selectedBorderColor, textColor, static value => value switch
        {
            DelayType.PingPong => "Ping-pong",
            _ => value.ToString()
        });
    }

    public static int DrawPresetFamilyButtons(
        Rectangle area,
        IReadOnlyList<GmInstrumentFamily> families,
        int selectedIndex,
        Vector2 mousePosition,
        bool mousePressed,
        Color panelColor,
        Color borderColor,
        Color selectedColor,
        Color selectedBorderColor,
        Color textColor)
    {
        const int columns = 4;
        const float gap = 10f;
        int rows = (int)MathF.Ceiling(families.Count / (float)columns);
        float buttonWidth = (area.Width - (gap * (columns - 1))) / columns;
        float buttonHeight = (area.Height - (gap * (rows - 1))) / rows;

        for (int i = 0; i < families.Count; i++)
        {
            int column = i % columns;
            int row = i / columns;
            Rectangle buttonBounds = new(area.X + (column * (buttonWidth + gap)), area.Y + (row * (buttonHeight + gap)), buttonWidth, buttonHeight);
            bool isSelected = selectedIndex == i;
            bool isHovered = Contains(buttonBounds, mousePosition);
            string label = FormatFamilyLabel(families[i]);
            Color fill = isSelected ? selectedColor : (isHovered ? new Color(245, 248, 255, 255) : panelColor);
            Color outline = isSelected ? selectedBorderColor : borderColor;
            const int fontSize = 18;

            Graphics.DrawRectangleRec(buttonBounds, fill);
            Graphics.DrawRectangleLinesEx(buttonBounds, isSelected ? 2.5f : 1f, outline);
            DrawCenteredWrappedLabel(buttonBounds, label, fontSize, textColor);

            if (mousePressed && isHovered)
            {
                selectedIndex = i;
            }
        }

        return selectedIndex;
    }

    public static int DrawPresetButtons(
        Rectangle area,
        IReadOnlyList<GmPreset> presets,
        int selectedIndex,
        Vector2 mousePosition,
        bool mousePressed,
        Color panelColor,
        Color borderColor,
        Color selectedColor,
        Color selectedBorderColor,
        Color textColor,
        Color mutedTextColor)
    {
        float gap = 12f;
        float buttonHeight = (area.Height - gap) / Math.Max(1, presets.Count);

        for (int i = 0; i < presets.Count; i++)
        {
            Rectangle buttonBounds = new(area.X, area.Y + (i * (buttonHeight + gap)), area.Width, buttonHeight);
            bool isSelected = selectedIndex == i;
            bool isHovered = Contains(buttonBounds, mousePosition);
            Color fill = isSelected ? selectedColor : (isHovered ? new Color(245, 248, 255, 255) : panelColor);
            Color outline = isSelected ? selectedBorderColor : borderColor;

            Graphics.DrawRectangleRec(buttonBounds, fill);
            Graphics.DrawRectangleLinesEx(buttonBounds, isSelected ? 2.5f : 1f, outline);
            Graphics.DrawText(presets[i].Name, (int)buttonBounds.X + 14, (int)buttonBounds.Y + 12, 20, textColor);
            DrawWrappedText(presets[i].Description, buttonBounds.X + 14, buttonBounds.Y + 40, buttonBounds.Width - 28, 16, mutedTextColor);

            if (mousePressed && isHovered)
            {
                selectedIndex = i;
            }
        }

        return selectedIndex;
    }

    public static float DrawSlider(
        int index,
        ref int activeSlider,
        bool enabled,
        string label,
        string valueLabel,
        Rectangle bounds,
        float value,
        float minValue,
        float maxValue,
        Vector2 mousePosition,
        bool mousePressed,
        bool mouseDown,
        Color accentColor,
        Color accentSoftColor,
        Color borderColor,
        Color panelColor,
        Color textColor,
        Color mutedTextColor)
    {
        Rectangle trackBounds = new(bounds.X, bounds.Y + 22, bounds.Width, bounds.Height);
        Color effectiveTextColor = enabled ? textColor : new Color(134, 143, 160, 255);
        Color effectiveMutedTextColor = enabled ? mutedTextColor : new Color(160, 168, 183, 255);
        Color effectivePanelColor = enabled ? panelColor : new Color(243, 245, 249, 255);
        Color effectiveBorderColor = enabled ? borderColor : new Color(205, 211, 222, 255);
        Color effectiveAccentSoftColor = enabled ? accentSoftColor : new Color(224, 228, 236, 255);
        Color effectiveAccentColor = enabled ? accentColor : new Color(182, 189, 201, 255);

        if (enabled && mousePressed && Contains(trackBounds, mousePosition))
        {
            activeSlider = index;
        }

        if (enabled && mouseDown && activeSlider == index)
        {
            float normalized = Math.Clamp((mousePosition.X - trackBounds.X) / trackBounds.Width, 0f, 1f);
            value = minValue + ((maxValue - minValue) * normalized);
        }

        float ratio = (value - minValue) / (maxValue - minValue);
        Rectangle fillBounds = new(trackBounds.X, trackBounds.Y, trackBounds.Width * ratio, trackBounds.Height);
        Rectangle thumbBounds = new(trackBounds.X + (trackBounds.Width * ratio) - 7f, trackBounds.Y - 4f, 14, trackBounds.Height + 8f);

        Graphics.DrawText(label, (int)bounds.X, (int)bounds.Y, 18, effectiveTextColor);
        Graphics.DrawText(valueLabel, (int)(bounds.X + bounds.Width - 60), (int)bounds.Y, 18, effectiveMutedTextColor);
        Graphics.DrawRectangleRec(trackBounds, effectivePanelColor);
        Graphics.DrawRectangleLinesEx(trackBounds, 1f, effectiveBorderColor);
        Graphics.DrawRectangleRec(fillBounds, effectiveAccentSoftColor);
        Graphics.DrawRectangleLinesEx(fillBounds, 0f, effectiveAccentSoftColor);
        Graphics.DrawRectangleRec(thumbBounds, effectiveAccentColor);

        return Math.Clamp(value, minValue, maxValue);
    }

    public static void DrawToggle(
        Rectangle bounds,
        string label,
        bool value,
        Color accentColor,
        Color accentSoftColor,
        Color borderColor,
        Color panelColor,
        Color textColor,
        Color mutedTextColor)
    {
        const int labelFontSize = 18;
        string stateLabel = value ? "On" : "Off";
        int stateWidth = TextManager.MeasureText(stateLabel, labelFontSize);
        int labelWidth = TextManager.MeasureText(label, labelFontSize);
        int labelX = (int)bounds.X;
        int labelY = (int)(bounds.Y + ((bounds.Height - labelFontSize) / 2f) - 1f);
        float preferredTrackX = labelX + labelWidth + 8 + stateWidth + 10;
        float trackX = MathF.Min(preferredTrackX, bounds.X + bounds.Width - 42f);
        Rectangle trackBounds = new(trackX, bounds.Y + ((bounds.Height - 22f) / 2f), 42, 22);
        int preferredStateX = labelX + labelWidth + 8;
        int maxStateX = (int)(trackBounds.X - 8f - stateWidth);
        int stateX = Math.Min(preferredStateX, maxStateX);

        Graphics.DrawText(label, labelX, labelY, labelFontSize, textColor);
        Graphics.DrawText(stateLabel, stateX, labelY, labelFontSize, mutedTextColor);

        float knobSize = 18f;
        float knobX = value ? trackBounds.X + trackBounds.Width - knobSize - 2f : trackBounds.X + 2f;
        Rectangle knobBounds = new(knobX, trackBounds.Y + 2f, knobSize, knobSize);

        Graphics.DrawRectangleRounded(trackBounds, 0.5f, 8, value ? accentSoftColor : panelColor);
        Graphics.DrawRectangleRoundedLinesEx(trackBounds, 0.5f, 8, 1.5f, value ? accentColor : borderColor);
        Graphics.DrawRectangleRounded(knobBounds, 0.5f, 8, value ? accentColor : mutedTextColor);
    }

    public static void DrawWaveformScope(Rectangle bounds, float[] samples, int writeIndex, Color waveColor, Color borderColor, Color labelColor)
    {
        Graphics.DrawText("Output waveform", (int)bounds.X + 18, (int)bounds.Y + 16, 22, labelColor);

        Rectangle graphBounds = new(bounds.X + 18, bounds.Y + 52, bounds.Width - 36, bounds.Height - 70);
        Graphics.DrawRectangleRec(graphBounds, new Color(246, 249, 255, 255));
        Graphics.DrawRectangleLinesEx(graphBounds, 1f, borderColor);

        Vector2 centerStart = new(graphBounds.X, graphBounds.Y + (graphBounds.Height / 2f));
        Vector2 centerEnd = new(graphBounds.X + graphBounds.Width, graphBounds.Y + (graphBounds.Height / 2f));
        Graphics.DrawLineV(centerStart, centerEnd, new Color(215, 223, 237, 255));

        int sampleCount = samples.Length;
        float xStep = graphBounds.Width / (sampleCount - 1);
        float centerY = graphBounds.Y + (graphBounds.Height / 2f);
        float amplitude = graphBounds.Height * 0.42f;

        Vector2 previous = new(graphBounds.X, centerY - (samples[writeIndex] * amplitude));

        for (int i = 1; i < sampleCount; i++)
        {
            int sampleIndex = (writeIndex + i) % sampleCount;
            Vector2 current = new(graphBounds.X + (xStep * i), centerY - (samples[sampleIndex] * amplitude));
            Graphics.DrawLineV(previous, current, waveColor);
            previous = current;
        }
    }

    public static void DrawFilterAnalysis(
        Rectangle bounds,
        float[] samples,
        int writeIndex,
        int sampleRate,
        FilterType filterType,
        float cutoffHz,
        float resonance,
        Color spectrumColor,
        Color responseColor,
        Color borderColor,
        Color labelColor)
    {
        Graphics.DrawText("Filter analysis", (int)bounds.X + 18, (int)bounds.Y + 16, 22, labelColor);
        Graphics.DrawText("Recent output spectrum", (int)bounds.X + 18, (int)bounds.Y + 42, 18, labelColor);

        Rectangle graphBounds = new(bounds.X + 18, bounds.Y + 76, bounds.Width - 36, bounds.Height - 94);
        Graphics.DrawRectangleRec(graphBounds, new Color(246, 249, 255, 255));
        Graphics.DrawRectangleLinesEx(graphBounds, 1f, borderColor);

        DrawFilterGrid(graphBounds, borderColor, labelColor);
        DrawSpectrumCurve(graphBounds, samples, writeIndex, sampleRate, spectrumColor);
        DrawFilterResponseCurve(graphBounds, sampleRate, filterType, cutoffHz, resonance, responseColor);
    }

    private static void DrawFilterGrid(Rectangle graphBounds, Color borderColor, Color labelColor)
    {
        float[] normalizedFrequencies = [0f, 0.25f, 0.5f, 0.75f, 1f];
        string[] frequencyLabels = ["20", "100", "1k", "5k", "20k"];

        for (int i = 0; i < normalizedFrequencies.Length; i++)
        {
            float x = graphBounds.X + (graphBounds.Width * normalizedFrequencies[i]);
            Graphics.DrawLineV(
                new Vector2(x, graphBounds.Y),
                new Vector2(x, graphBounds.Y + graphBounds.Height),
                new Color(225, 231, 242, 255));
            Graphics.DrawText(frequencyLabels[i], (int)x - 14, (int)graphBounds.Y + (int)graphBounds.Height + 6, 16, labelColor);
        }

        for (int i = 1; i < 4; i++)
        {
            float y = graphBounds.Y + ((graphBounds.Height / 4f) * i);
            Graphics.DrawLineV(
                new Vector2(graphBounds.X, y),
                new Vector2(graphBounds.X + graphBounds.Width, y),
                new Color(225, 231, 242, 255));
        }

        Graphics.DrawText("0 dB", (int)graphBounds.X + 8, (int)graphBounds.Y + 6, 16, labelColor);
        Graphics.DrawText("-48 dB", (int)graphBounds.X + 8, (int)graphBounds.Y + (int)graphBounds.Height - 22, 16, labelColor);
    }

    private static void DrawSpectrumCurve(Rectangle graphBounds, float[] samples, int writeIndex, int sampleRate, Color spectrumColor)
    {
        const int analysisSize = 256;
        const int pointCount = 96;
        const float minFrequency = 20f;

        int availableSamples = Math.Min(samples.Length, analysisSize);
        if (availableSamples < 32)
        {
            return;
        }

        Vector2? previousPoint = null;

        for (int pointIndex = 0; pointIndex < pointCount; pointIndex++)
        {
            float normalizedX = pointIndex / (pointCount - 1f);
            float frequency = minFrequency * MathF.Pow((sampleRate * 0.5f) / minFrequency, normalizedX);
            float magnitude = MeasureMagnitude(samples, writeIndex, availableSamples, frequency, sampleRate);
            float decibels = 20f * MathF.Log10(MathF.Max(magnitude, 0.00001f));
            float normalizedMagnitude = Math.Clamp((decibels + 48f) / 48f, 0f, 1f);

            Vector2 point = new(
                graphBounds.X + (graphBounds.Width * normalizedX),
                graphBounds.Y + graphBounds.Height - (graphBounds.Height * normalizedMagnitude));

            if (previousPoint is Vector2 previous)
            {
                Graphics.DrawLineV(previous, point, spectrumColor);
            }

            previousPoint = point;
        }
    }

    private static float MeasureMagnitude(float[] samples, int writeIndex, int sampleCount, float frequency, int sampleRate)
    {
        float real = 0f;
        float imaginary = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            int sampleIndex = (writeIndex - sampleCount + i + samples.Length) % samples.Length;
            float sample = samples[sampleIndex];
            float window = 0.5f - (0.5f * MathF.Cos(MathF.Tau * i / (sampleCount - 1f)));
            float phase = MathF.Tau * frequency * i / sampleRate;
            float weightedSample = sample * window;

            real += weightedSample * MathF.Cos(phase);
            imaginary -= weightedSample * MathF.Sin(phase);
        }

        return MathF.Sqrt((real * real) + (imaginary * imaginary)) / sampleCount;
    }

    private static void DrawFilterResponseCurve(Rectangle graphBounds, int sampleRate, FilterType filterType, float cutoffHz, float resonance, Color responseColor)
    {
        const int pointCount = 96;
        const float minFrequency = 20f;

        Vector2? previousPoint = null;

        for (int pointIndex = 0; pointIndex < pointCount; pointIndex++)
        {
            float normalizedX = pointIndex / (pointCount - 1f);
            float frequency = minFrequency * MathF.Pow((sampleRate * 0.5f) / minFrequency, normalizedX);
            float magnitude = CalculateFilterResponseMagnitude(sampleRate, filterType, cutoffHz, resonance, frequency);
            float decibels = 20f * MathF.Log10(MathF.Max(magnitude, 0.00001f));
            float normalizedMagnitude = Math.Clamp((decibels + 48f) / 48f, 0f, 1f);

            Vector2 point = new(
                graphBounds.X + (graphBounds.Width * normalizedX),
                graphBounds.Y + graphBounds.Height - (graphBounds.Height * normalizedMagnitude));

            if (previousPoint is Vector2 previous)
            {
                Graphics.DrawLineEx(previous, point, 2f, responseColor);
            }

            previousPoint = point;
        }
    }

    private static float CalculateFilterResponseMagnitude(int sampleRate, FilterType filterType, float cutoffHz, float resonance, float frequency)
    {
        if (filterType == FilterType.Off)
        {
            return 1f;
        }

        cutoffHz = Math.Clamp(cutoffHz, 20f, sampleRate * 0.45f);
        resonance = Math.Clamp(resonance, 0f, 1f);

        float q = 0.707f + ((8f - 0.707f) * resonance * resonance);
        float omega = MathF.Tau * cutoffHz / sampleRate;
        float sinOmega = MathF.Sin(omega);
        float cosOmega = MathF.Cos(omega);
        float alpha = sinOmega / (2f * q);

        float b0;
        float b1;
        float b2;
        float a0 = 1f + alpha;
        float a1 = -2f * cosOmega;
        float a2 = 1f - alpha;

        switch (filterType)
        {
            case FilterType.LowPass:
                b0 = (1f - cosOmega) * 0.5f;
                b1 = 1f - cosOmega;
                b2 = (1f - cosOmega) * 0.5f;
                break;

            case FilterType.HighPass:
                b0 = (1f + cosOmega) * 0.5f;
                b1 = -(1f + cosOmega);
                b2 = (1f + cosOmega) * 0.5f;
                break;

            case FilterType.BandPass:
                b0 = alpha;
                b1 = 0f;
                b2 = -alpha;
                break;

            default:
                return 1f;
        }

        float normalizedB0 = b0 / a0;
        float normalizedB1 = b1 / a0;
        float normalizedB2 = b2 / a0;
        float normalizedA1 = a1 / a0;
        float normalizedA2 = a2 / a0;

        float theta = MathF.Tau * frequency / sampleRate;
        float cosTheta = MathF.Cos(theta);
        float sinTheta = MathF.Sin(theta);
        float cosTwoTheta = MathF.Cos(theta * 2f);
        float sinTwoTheta = MathF.Sin(theta * 2f);

        float numeratorReal = normalizedB0 + (normalizedB1 * cosTheta) + (normalizedB2 * cosTwoTheta);
        float numeratorImaginary = -(normalizedB1 * sinTheta) - (normalizedB2 * sinTwoTheta);
        float denominatorReal = 1f + (normalizedA1 * cosTheta) + (normalizedA2 * cosTwoTheta);
        float denominatorImaginary = -(normalizedA1 * sinTheta) - (normalizedA2 * sinTwoTheta);

        float numeratorMagnitude = MathF.Sqrt((numeratorReal * numeratorReal) + (numeratorImaginary * numeratorImaginary));
        float denominatorMagnitude = MathF.Sqrt((denominatorReal * denominatorReal) + (denominatorImaginary * denominatorImaginary));

        return denominatorMagnitude <= 0.00001f ? 0f : numeratorMagnitude / denominatorMagnitude;
    }

    public static void DrawKeyboard(
        PianoKeyLayout[] keys,
        IReadOnlySet<int> activeNotes,
        int hoveredNote,
        Color whiteKeyColor,
        Color borderColor,
        Color blackKeyColor,
        Color activeWhiteKeyColor,
        Color activeBlackKeyColor,
        Color textColor)
    {
        foreach (PianoKeyLayout key in keys.Where(static key => !key.IsBlack))
        {
            bool isActive = activeNotes.Contains(key.MidiNote);
            bool isHovered = key.MidiNote == hoveredNote;
            Color fill = isActive ? activeWhiteKeyColor : (isHovered ? new Color(242, 247, 255, 255) : whiteKeyColor);

            Graphics.DrawRectangleRec(key.Bounds, fill);
            Graphics.DrawRectangleLinesEx(key.Bounds, 1f, borderColor);

            if (key.MidiNote % 12 == 0 || key.MidiNote == 21)
            {
                const int labelFontSize = 14;
                int labelWidth = TextManager.MeasureText(key.Label, labelFontSize);
                int labelX = (int)(key.Bounds.X + ((key.Bounds.Width - labelWidth) / 2f));
                int labelY = (int)(key.Bounds.Y + key.Bounds.Height - 22);
                Graphics.DrawText(key.Label, labelX, labelY, labelFontSize, textColor);
            }
        }

        foreach (PianoKeyLayout key in keys.Where(static key => key.IsBlack))
        {
            bool isActive = activeNotes.Contains(key.MidiNote);
            bool isHovered = key.MidiNote == hoveredNote;
            Color fill = isActive ? activeBlackKeyColor : (isHovered ? new Color(70, 78, 99, 255) : blackKeyColor);

            Graphics.DrawRectangleRec(key.Bounds, fill);
            Graphics.DrawRectangleLinesEx(key.Bounds, 1f, new Color(18, 22, 30, 255));
        }
    }

    public static void DrawPanel(Rectangle bounds, Color fillColor, Color outlineColor)
    {
        Graphics.DrawRectangleRec(bounds, fillColor);
        Graphics.DrawRectangleLinesEx(bounds, 1f, outlineColor);
    }

    public static bool Contains(Rectangle bounds, Vector2 point)
    {
        return point.X >= bounds.X
            && point.X <= bounds.X + bounds.Width
            && point.Y >= bounds.Y
            && point.Y <= bounds.Y + bounds.Height;
    }

    private static TEnum DrawEnumButtons<TEnum>(
        Rectangle area,
        ReadOnlySpan<TEnum> values,
        TEnum currentValue,
        Vector2 mousePosition,
        bool mousePressed,
        Color panelColor,
        Color borderColor,
        Color selectedColor,
        Color selectedBorderColor,
        Color textColor,
        Func<TEnum, string> labelSelector)
        where TEnum : struct, Enum
    {
        float buttonGap = 10f;
        float buttonWidth = (area.Width - (buttonGap * (values.Length - 1))) / values.Length;

        for (int i = 0; i < values.Length; i++)
        {
            Rectangle buttonBounds = new(area.X + (i * (buttonWidth + buttonGap)), area.Y, buttonWidth, area.Height);
            bool isSelected = EqualityComparer<TEnum>.Default.Equals(currentValue, values[i]);
            bool isHovered = Contains(buttonBounds, mousePosition);
            string buttonLabel = labelSelector(values[i]);
            const int buttonFontSize = 18;
            Color fill = isSelected ? selectedColor : (isHovered ? new Color(245, 248, 255, 255) : panelColor);
            Color outline = isSelected ? selectedBorderColor : borderColor;

            Graphics.DrawRectangleRec(buttonBounds, fill);
            Graphics.DrawRectangleLinesEx(buttonBounds, isSelected ? 2.5f : 1f, outline);
            int textWidth = TextManager.MeasureText(buttonLabel, buttonFontSize);
            int textX = (int)(buttonBounds.X + ((buttonBounds.Width - textWidth) / 2f));
            int textY = (int)(buttonBounds.Y + ((buttonBounds.Height - buttonFontSize) / 2f) - 1f);
            Graphics.DrawText(buttonLabel, textX, textY, buttonFontSize, textColor);

            if (mousePressed && isHovered)
            {
                currentValue = values[i];
            }
        }

        return currentValue;
    }

    private static string FormatFamilyLabel(GmInstrumentFamily family)
    {
        return family switch
        {
            GmInstrumentFamily.ChromaticPercussion => "Chromatic\nPerc.",
            GmInstrumentFamily.SynthLead => "Synth\nLead",
            GmInstrumentFamily.SynthPad => "Synth\nPad",
            GmInstrumentFamily.SynthEffects => "Synth\nFX",
            GmInstrumentFamily.SoundEffects => "Sound\nFX",
            _ => family.ToString()
        };
    }

    private static void DrawCenteredWrappedLabel(Rectangle bounds, string label, int fontSize, Color textColor)
    {
        string[] lines = label.Split('\n');
        float totalHeight = lines.Length * fontSize;
        float startY = bounds.Y + ((bounds.Height - totalHeight) / 2f) - 2f;

        for (int i = 0; i < lines.Length; i++)
        {
            int lineWidth = TextManager.MeasureText(lines[i], fontSize);
            int textX = (int)(bounds.X + ((bounds.Width - lineWidth) / 2f));
            int textY = (int)(startY + (i * fontSize));
            Graphics.DrawText(lines[i], textX, textY, fontSize, textColor);
        }
    }

    private static void DrawWrappedText(string text, float x, float y, float maxWidth, int fontSize, Color textColor)
    {
        string[] words = text.Split(' ');
        string currentLine = string.Empty;
        float currentY = y;

        foreach (string word in words)
        {
            string candidate = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
            if (!string.IsNullOrEmpty(currentLine) && TextManager.MeasureText(candidate, fontSize) > maxWidth)
            {
                Graphics.DrawText(currentLine, (int)x, (int)currentY, fontSize, textColor);
                currentLine = word;
                currentY += fontSize + 2f;
            }
            else
            {
                currentLine = candidate;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            Graphics.DrawText(currentLine, (int)x, (int)currentY, fontSize, textColor);
        }
    }
}
