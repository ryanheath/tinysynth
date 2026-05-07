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
        Waveform[] waveforms = [Waveform.Sine, Waveform.Square, Waveform.Saw, Waveform.Triangle];
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
        Graphics.DrawText("Recent audio samples from the mixed synth output", (int)bounds.X + 18, (int)bounds.Y + 46, 18, labelColor);

        Rectangle graphBounds = new(bounds.X + 18, bounds.Y + 82, bounds.Width - 36, bounds.Height - 100);
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

            if (key.MidiNote % 12 == 0)
            {
                Graphics.DrawText(key.Label, (int)key.Bounds.X + 10, (int)(key.Bounds.Y + key.Bounds.Height - 28), 18, textColor);
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
}
