using Raylib_CSharp.Colors;
using Raylib_CSharp.Rendering;
using Raylib_CSharp.Transformations;
using System.Numerics;
using TinySynth.Synth;

namespace TinySynth.UI;

internal static class SynthRenderer
{
    public static Waveform DrawWaveformButtons(
        Rectangle area,
        Waveform currentValue,
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
            Color fill = isSelected ? selectedColor : (isHovered ? new Color(245, 248, 255, 255) : panelColor);
            Color outline = isSelected ? selectedBorderColor : borderColor;

            Graphics.DrawRectangleRec(buttonBounds, fill);
            Graphics.DrawRectangleLinesEx(buttonBounds, isSelected ? 2.5f : 1f, outline);
            Graphics.DrawText(waveforms[i].ToString(), (int)buttonBounds.X + 18, (int)buttonBounds.Y + 8, 18, textColor);

            if (mousePressed && isHovered)
            {
                currentValue = waveforms[i];
            }
        }

        return currentValue;
    }

    public static float DrawSlider(
        int index,
        ref int activeSlider,
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

        if (mousePressed && Contains(trackBounds, mousePosition))
        {
            activeSlider = index;
        }

        if (mouseDown && activeSlider == index)
        {
            float normalized = Math.Clamp((mousePosition.X - trackBounds.X) / trackBounds.Width, 0f, 1f);
            value = minValue + ((maxValue - minValue) * normalized);
        }

        float ratio = (value - minValue) / (maxValue - minValue);
        Rectangle fillBounds = new(trackBounds.X, trackBounds.Y, trackBounds.Width * ratio, trackBounds.Height);
        Rectangle thumbBounds = new(trackBounds.X + (trackBounds.Width * ratio) - 7f, trackBounds.Y - 4f, 14, trackBounds.Height + 8f);

        Graphics.DrawText(label, (int)bounds.X, (int)bounds.Y, 18, textColor);
        Graphics.DrawText(valueLabel, (int)(bounds.X + bounds.Width - 60), (int)bounds.Y, 18, mutedTextColor);
        Graphics.DrawRectangleRec(trackBounds, panelColor);
        Graphics.DrawRectangleLinesEx(trackBounds, 1f, borderColor);
        Graphics.DrawRectangleRec(fillBounds, accentSoftColor);
        Graphics.DrawRectangleLinesEx(fillBounds, 0f, accentSoftColor);
        Graphics.DrawRectangleRec(thumbBounds, accentColor);

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
        Graphics.DrawText(label, (int)bounds.X, (int)bounds.Y - 1, 18, textColor);
        Graphics.DrawText(value ? "On" : "Off", (int)bounds.X + 78, (int)bounds.Y - 1, 18, mutedTextColor);

        Rectangle trackBounds = new(bounds.X + 84, bounds.Y + 1, 42, 22);
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
        Graphics.DrawText("Recent audio samples from the active synth voice", (int)bounds.X + 18, (int)bounds.Y + 46, 18, labelColor);

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
        int activeNote,
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
            bool isActive = key.MidiNote == activeNote;
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
            bool isActive = key.MidiNote == activeNote;
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
