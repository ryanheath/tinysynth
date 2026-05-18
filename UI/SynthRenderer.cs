using Raylib_CSharp.Colors;
using Raylib_CSharp.Fonts;
using Raylib_CSharp.Rendering;
using Raylib_CSharp.Transformations;
using System.Numerics;
using TinySynth.App;
using TinySynth.Synth;

namespace TinySynth.UI;

internal static class SynthRenderer
{
    public static int DrawOscillatorButtons(
        Rectangle area,
        IReadOnlyList<OscillatorParameters> oscillators,
        int activeOscillatorIndex,
        Vector2 mousePosition,
        bool mousePressed)
    {
        float buttonGap = 10f;
        float buttonWidth = (area.Width - (buttonGap * (SynthParameters.OscillatorCount - 1))) / SynthParameters.OscillatorCount;

        for (int i = 0; i < SynthParameters.OscillatorCount; i++)
        {
            Rectangle buttonBounds = new(area.X + (i * (buttonWidth + buttonGap)), area.Y, buttonWidth, area.Height);
            bool isEnabled = oscillators[i].Enabled;
            bool isSelected = activeOscillatorIndex == i;
            bool isHovered = UiHitTesting.Contains(buttonBounds, mousePosition);
            Color fill = isEnabled
                ? (isSelected ? UiTheme.AccentSoftColor : (isHovered ? UiTheme.PanelHoverColor : UiTheme.PanelColor))
                : UiTheme.DisabledFillColor;
            Color outline = isEnabled
                ? (isSelected ? UiTheme.AccentStrongColor : UiTheme.BorderColor)
                : UiTheme.DisabledBorderColor;
            Color buttonTextColor = isEnabled ? UiTheme.TextColor : UiTheme.DisabledTextColor;

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
        bool mousePressed)
    {
        Waveform[] waveforms = [
            Waveform.Sine,
            Waveform.Square,
            Waveform.Pulse,
            Waveform.Saw,
            Waveform.Triangle,
            Waveform.Noise,
            Waveform.SuperSaw,
            Waveform.Organ,
            Waveform.Metallic,
            Waveform.PinkNoise
        ];
        float buttonGap = 10f;
        const int columnCount = 5;
        int rowCount = (int)MathF.Ceiling(waveforms.Length / (float)columnCount);
        float buttonWidth = (area.Width - (buttonGap * (columnCount - 1))) / columnCount;
        float buttonHeight = (area.Height - (buttonGap * (rowCount - 1))) / rowCount;

        for (int i = 0; i < waveforms.Length; i++)
        {
            int column = i % columnCount;
            int row = i / columnCount;
            Rectangle buttonBounds = new(area.X + (column * (buttonWidth + buttonGap)), area.Y + (row * (buttonHeight + buttonGap)), buttonWidth, buttonHeight);
            bool isSelected = currentValue == waveforms[i];
            bool isHovered = UiHitTesting.Contains(buttonBounds, mousePosition);
            Color fill = enabled
                ? (isSelected ? UiTheme.AccentSoftColor : (isHovered ? UiTheme.PanelHoverColor : UiTheme.PanelColor))
                : UiTheme.DisabledFillColor;
            Color outline = enabled
                ? (isSelected ? UiTheme.AccentStrongColor : UiTheme.BorderColor)
                : UiTheme.DisabledBorderColor;
            Color buttonTextColor = enabled ? UiTheme.TextColor : UiTheme.DisabledTextColor;

            Graphics.DrawRectangleRec(buttonBounds, fill);
            Graphics.DrawRectangleLinesEx(buttonBounds, isSelected ? 2.5f : 1f, outline);
            DrawWaveformPreview(buttonBounds, waveforms[i], buttonTextColor, outline);

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
        bool mousePressed)
    {
        string[] labels = ["Presets", "Oscillator", "Filter", "Mod", "FX"];
        float buttonGap = 10f;
        float buttonWidth = (area.Width - (buttonGap * (labels.Length - 1))) / labels.Length;

        for (int i = 0; i < labels.Length; i++)
        {
            Rectangle buttonBounds = new(area.X + (i * (buttonWidth + buttonGap)), area.Y, buttonWidth, area.Height);
            bool isSelected = selectedIndex == i;
            bool isHovered = UiHitTesting.Contains(buttonBounds, mousePosition);
            const int buttonFontSize = 18;
            Color fill = isSelected ? UiTheme.AccentSoftColor : (isHovered ? UiTheme.PanelHoverColor : UiTheme.PanelColor);
            Color outline = isSelected ? UiTheme.AccentStrongColor : UiTheme.BorderColor;

            Graphics.DrawRectangleRec(buttonBounds, fill);
            Graphics.DrawRectangleLinesEx(buttonBounds, isSelected ? 2.5f : 1f, outline);
            int textWidth = TextManager.MeasureText(labels[i], buttonFontSize);
            int textX = (int)(buttonBounds.X + ((buttonBounds.Width - textWidth) / 2f));
            int textY = (int)(buttonBounds.Y + ((buttonBounds.Height - buttonFontSize) / 2f) - 1f);
            Graphics.DrawText(labels[i], textX, textY, buttonFontSize, UiTheme.TextColor);

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
        bool mousePressed)
    {
        FilterType[] filterTypes = [FilterType.Off, FilterType.LowPass, FilterType.HighPass, FilterType.BandPass];
        float buttonGap = 10f;
        float buttonWidth = (area.Width - (buttonGap * (filterTypes.Length - 1))) / filterTypes.Length;

        for (int i = 0; i < filterTypes.Length; i++)
        {
            Rectangle buttonBounds = new(area.X + (i * (buttonWidth + buttonGap)), area.Y, buttonWidth, area.Height);
            bool isSelected = currentValue == filterTypes[i];
            bool isHovered = UiHitTesting.Contains(buttonBounds, mousePosition);
            string buttonLabel = filterTypes[i] switch
            {
                FilterType.Off => "Off",
                FilterType.LowPass => "Low-pass",
                FilterType.HighPass => "High-pass",
                FilterType.BandPass => "Band-pass",
                _ => filterTypes[i].ToString()
            };
            const int buttonFontSize = 18;
            Color fill = isSelected ? UiTheme.AccentSoftColor : (isHovered ? UiTheme.PanelHoverColor : UiTheme.PanelColor);
            Color outline = isSelected ? UiTheme.AccentStrongColor : UiTheme.BorderColor;

            Graphics.DrawRectangleRec(buttonBounds, fill);
            Graphics.DrawRectangleLinesEx(buttonBounds, isSelected ? 2.5f : 1f, outline);
            int textWidth = TextManager.MeasureText(buttonLabel, buttonFontSize);
            int textX = (int)(buttonBounds.X + ((buttonBounds.Width - textWidth) / 2f));
            int textY = (int)(buttonBounds.Y + ((buttonBounds.Height - buttonFontSize) / 2f) - 1f);
            Graphics.DrawText(buttonLabel, textX, textY, buttonFontSize, UiTheme.TextColor);

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
        bool mousePressed)
    {
        ChorusType[] chorusTypes = [ChorusType.Off, ChorusType.Light, ChorusType.Ensemble, ChorusType.Wide];
        return DrawEnumButtons(area, chorusTypes, currentValue, mousePosition, mousePressed, static value => value.ToString());
    }

    public static ReverbType DrawReverbButtons(
        Rectangle area,
        ReverbType currentValue,
        Vector2 mousePosition,
        bool mousePressed)
    {
        ReverbType[] reverbTypes = [ReverbType.Off, ReverbType.Room, ReverbType.Hall, ReverbType.Shimmer];
        return DrawEnumButtons(area, reverbTypes, currentValue, mousePosition, mousePressed, static value => value.ToString());
    }

    public static DelayType DrawDelayButtons(
        Rectangle area,
        DelayType currentValue,
        Vector2 mousePosition,
        bool mousePressed)
    {
        DelayType[] delayTypes = [DelayType.Off, DelayType.Slap, DelayType.PingPong, DelayType.Tape];
        return DrawEnumButtons(area, delayTypes, currentValue, mousePosition, mousePressed, static value => value switch
        {
            DelayType.PingPong => "Ping-pong",
            _ => value.ToString()
        });
    }

    public static EnvelopeMode DrawEnvelopeModeButtons(
        Rectangle area,
        EnvelopeMode currentValue,
        bool enabled,
        Vector2 mousePosition,
        bool mousePressed)
    {
        EnvelopeMode[] envelopeModes = [EnvelopeMode.Sustain, EnvelopeMode.OneShot];
        return DrawEnumButtons(
            area,
            envelopeModes,
            currentValue,
            mousePosition,
            enabled && mousePressed,
            static value => value switch
            {
                EnvelopeMode.OneShot => "One-shot",
                _ => "Sustain"
            });
    }

    public static ModulationLfoShape DrawModulationLfoShapeButtons(
        Rectangle area,
        ModulationLfoShape currentValue,
        Vector2 mousePosition,
        bool mousePressed)
    {
        ModulationLfoShape[] lfoShapes = [ModulationLfoShape.Sine, ModulationLfoShape.Triangle, ModulationLfoShape.Saw, ModulationLfoShape.Square];
        float buttonGap = 8f;
        float buttonWidth = (area.Width - (buttonGap * (lfoShapes.Length - 1))) / lfoShapes.Length;

        for (int i = 0; i < lfoShapes.Length; i++)
        {
            Rectangle buttonBounds = new(area.X + (i * (buttonWidth + buttonGap)), area.Y, buttonWidth, area.Height);
            bool isSelected = currentValue == lfoShapes[i];
            bool isHovered = UiHitTesting.Contains(buttonBounds, mousePosition);
            Color fill = isSelected ? UiTheme.AccentSoftColor : (isHovered ? UiTheme.PanelHoverColor : UiTheme.PanelColor);
            Color outline = isSelected ? UiTheme.AccentStrongColor : UiTheme.BorderColor;

            Graphics.DrawRectangleRec(buttonBounds, fill);
            Graphics.DrawRectangleLinesEx(buttonBounds, isSelected ? 2.5f : 1f, outline);
            DrawModulationShapePreview(buttonBounds, lfoShapes[i], UiTheme.TextColor, outline);

            if (mousePressed && isHovered)
            {
                currentValue = lfoShapes[i];
            }
        }

        return currentValue;
    }

    public static float DrawKnobSlider(
        UiControlId index,
        ref UiControlId? activeSlider,
        bool enabled,
        string label,
        string valueLabel,
        Rectangle bounds,
        float value,
        float minValue,
        float maxValue,
        Vector2 mousePosition,
        bool mousePressed,
        bool mouseDown)
    {
        Rectangle knobBounds = new(bounds.X + ((bounds.Width - 34f) / 2f), bounds.Y + 22, 34f, 34f);
        Rectangle dragBounds = new(bounds.X, knobBounds.Y - 12f, bounds.Width, knobBounds.Height + 24f);
        Color effectiveTextColor = enabled ? UiTheme.TextColor : UiTheme.DisabledTextColor;
        Color effectiveMutedTextColor = enabled ? UiTheme.MutedTextColor : UiTheme.DisabledMutedTextColor;
        Color effectivePanelColor = enabled ? UiTheme.PanelColor : UiTheme.DisabledPanelColor;
        Color effectiveBorderColor = enabled ? UiTheme.BorderColor : UiTheme.DisabledBorderColor;
        Color effectiveAccentSoftColor = enabled ? UiTheme.AccentSoftColor : UiTheme.DisabledAccentSoftColor;
        Color effectiveAccentColor = enabled ? UiTheme.AccentColor : UiTheme.DisabledAccentColor;

        if (enabled && mousePressed && UiHitTesting.Contains(knobBounds, mousePosition))
        {
            activeSlider = index;
        }

        if (enabled && mouseDown && activeSlider == index)
        {
            float normalized = GetKnobDragRatio(mousePosition, knobBounds, dragBounds);
            value = minValue + ((maxValue - minValue) * normalized);
        }

        float ratio = (value - minValue) / (maxValue - minValue);

        Graphics.DrawText(label, (int)bounds.X, (int)bounds.Y, 18, effectiveTextColor);
        Graphics.DrawText(valueLabel, (int)(bounds.X + bounds.Width - 60), (int)bounds.Y, 18, effectiveMutedTextColor);
        DrawKnob(knobBounds, ratio, effectivePanelColor, effectiveBorderColor, effectiveAccentSoftColor, effectiveAccentColor);

        return Math.Clamp(value, minValue, maxValue);
    }

    public static ModulationSource DrawModulationSourceCycler(
        Rectangle area,
        ModulationSource currentValue,
        bool enabled,
        Vector2 mousePosition,
        bool mousePressed)
    {
        return DrawEnumCycler(area, currentValue, enabled, mousePosition, mousePressed, static value => value switch
        {
            ModulationSource.Lfo1 => "LFO 1",
            ModulationSource.Lfo2 => "LFO 2",
            ModulationSource.Envelope => "Envelope",
            ModulationSource.KeyTrack => "Key track",
            _ => "None"
        });
    }

    public static ModulationDestination DrawModulationDestinationCycler(
        Rectangle area,
        ModulationDestination currentValue,
        bool enabled,
        Vector2 mousePosition,
        bool mousePressed)
    {
        return DrawEnumCycler(area, currentValue, enabled, mousePosition, mousePressed, static value => value switch
        {
            ModulationDestination.FilterCutoff => "Cutoff",
            ModulationDestination.FilterResonance => "Resonance",
            ModulationDestination.PulseWidth => "Pulse",
            ModulationDestination.Lfo1Rate => "LFO 1 rate",
            ModulationDestination.Lfo2Rate => "LFO 2 rate",
            ModulationDestination.ChorusMix => "Chorus mix",
            ModulationDestination.DelayMix => "Delay mix",
            ModulationDestination.ReverbMix => "Reverb mix",
            _ => value.ToString()
        });
    }

    public static int DrawModulationOscillatorTargetCycler(
        Rectangle area,
        int currentValue,
        bool enabled,
        Vector2 mousePosition,
        bool mousePressed)
    {
        string[] labels = ["All", "Osc 1", "Osc 2", "Osc 3", "Osc 4"];
        int normalizedValue = Math.Clamp(currentValue + 1, 0, labels.Length - 1);
        normalizedValue = DrawIndexedCycler(area, normalizedValue, enabled, mousePosition, mousePressed, labels);
        return normalizedValue - 1;
    }

    public static int DrawPresetFamilyButtons(
        Rectangle area,
        IReadOnlyList<GmInstrumentFamily> families,
        int selectedIndex,
        Vector2 mousePosition,
        bool mousePressed)
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
            bool isHovered = UiHitTesting.Contains(buttonBounds, mousePosition);
            string label = FormatFamilyLabel(families[i]);
            Color fill = isSelected ? UiTheme.AccentSoftColor : (isHovered ? new Color(245, 248, 255, 255) : UiTheme.PanelColor);
            Color outline = isSelected ? UiTheme.AccentStrongColor : UiTheme.BorderColor;
            const int fontSize = 18;

            Graphics.DrawRectangleRec(buttonBounds, fill);
            Graphics.DrawRectangleLinesEx(buttonBounds, isSelected ? 2.5f : 1f, outline);
            DrawCenteredWrappedLabel(buttonBounds, label, fontSize, UiTheme.TextColor);

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
        bool mousePressed)
    {
        float gap = 12f;
        float buttonHeight = (area.Height - gap) / Math.Max(1, presets.Count);

        for (int i = 0; i < presets.Count; i++)
        {
            Rectangle buttonBounds = new(area.X, area.Y + (i * (buttonHeight + gap)), area.Width, buttonHeight);
            bool isSelected = selectedIndex == i;
            bool isHovered = UiHitTesting.Contains(buttonBounds, mousePosition);
            Color fill = isSelected ? UiTheme.AccentSoftColor : (isHovered ? new Color(245, 248, 255, 255) : UiTheme.PanelColor);
            Color outline = isSelected ? UiTheme.AccentStrongColor : UiTheme.BorderColor;

            Graphics.DrawRectangleRec(buttonBounds, fill);
            Graphics.DrawRectangleLinesEx(buttonBounds, isSelected ? 2.5f : 1f, outline);
            Graphics.DrawText(presets[i].Name, (int)buttonBounds.X + 14, (int)buttonBounds.Y + 6, 20, UiTheme.TextColor);
            DrawWrappedText(presets[i].Description, buttonBounds.X + 14, buttonBounds.Y + 28, buttonBounds.Width - 28, 16, UiTheme.MutedTextColor);

            if (mousePressed && isHovered)
            {
                selectedIndex = i;
            }
        }

        return selectedIndex;
    }

    public static float DrawSlider(
        UiControlId index,
        ref UiControlId? activeSlider,
        bool enabled,
        string label,
        string valueLabel,
        Rectangle bounds,
        float value,
        float minValue,
        float maxValue,
        Vector2 mousePosition,
        bool mousePressed,
        bool mouseDown)
    {
        Rectangle trackBounds = new(bounds.X, bounds.Y + 22, bounds.Width, bounds.Height);
        Color effectiveTextColor = enabled ? UiTheme.TextColor : UiTheme.DisabledTextColor;
        Color effectiveMutedTextColor = enabled ? UiTheme.MutedTextColor : UiTheme.DisabledMutedTextColor;
        Color effectivePanelColor = enabled ? UiTheme.PanelColor : UiTheme.DisabledPanelColor;
        Color effectiveBorderColor = enabled ? UiTheme.BorderColor : UiTheme.DisabledBorderColor;
        Color effectiveAccentSoftColor = enabled ? UiTheme.AccentSoftColor : UiTheme.DisabledAccentSoftColor;
        Color effectiveAccentColor = enabled ? UiTheme.AccentColor : UiTheme.DisabledAccentColor;

        if (enabled && mousePressed && UiHitTesting.Contains(trackBounds, mousePosition))
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
        bool value)
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

        Graphics.DrawText(label, labelX, labelY, labelFontSize, UiTheme.TextColor);
        Graphics.DrawText(stateLabel, stateX, labelY, labelFontSize, UiTheme.MutedTextColor);

        float knobSize = 18f;
        float knobX = value ? trackBounds.X + trackBounds.Width - knobSize - 2f : trackBounds.X + 2f;
        Rectangle knobBounds = new(knobX, trackBounds.Y + 2f, knobSize, knobSize);

        Graphics.DrawRectangleRounded(trackBounds, 0.5f, 8, value ? UiTheme.AccentSoftColor : UiTheme.PanelColor);
        Graphics.DrawRectangleRoundedLinesEx(trackBounds, 0.5f, 8, 1.5f, value ? UiTheme.AccentColor : UiTheme.BorderColor);
        Graphics.DrawRectangleRounded(knobBounds, 0.5f, 8, value ? UiTheme.AccentColor : UiTheme.MutedTextColor);
    }

    private static void DrawModulationShapePreview(Rectangle bounds, ModulationLfoShape shape, Color waveColor, Color borderColor)
    {
        Rectangle previewBounds = new(bounds.X + 6f, bounds.Y + 7f, bounds.Width - 12f, bounds.Height - 14f);
        float centerY = previewBounds.Y + (previewBounds.Height / 2f);
        Graphics.DrawLineEx(new Vector2(previewBounds.X, centerY), new Vector2(previewBounds.X + previewBounds.Width, centerY), 1f, UiTheme.PreviewGuideColor);

        const int pointCount = 18;
        Vector2 previousPoint = new(previewBounds.X, GetModulationShapePreviewY(shape, 0f, previewBounds));
        for (int i = 1; i < pointCount; i++)
        {
            float t = i / (pointCount - 1f);
            Vector2 point = new(previewBounds.X + (previewBounds.Width * t), GetModulationShapePreviewY(shape, t, previewBounds));
            Graphics.DrawLineEx(previousPoint, point, 2f, waveColor);
            previousPoint = point;
        }
    }

    private static float GetModulationShapePreviewY(ModulationLfoShape shape, float t, Rectangle bounds)
    {
        float amplitude = bounds.Height * 0.38f;
        float centerY = bounds.Y + (bounds.Height / 2f);
        float phase = t * MathF.Tau;
        float sample = shape switch
        {
            ModulationLfoShape.Sine => MathF.Sin(phase),
            ModulationLfoShape.Triangle => 1f - (4f * MathF.Abs(t - 0.5f)),
            ModulationLfoShape.Saw => 1f - (2f * t),
            ModulationLfoShape.Square => MathF.Sin(phase) >= 0f ? 1f : -1f,
            _ => MathF.Sin(phase)
        };

        return centerY - (sample * amplitude);
    }

    public static bool DrawCheckbox(
        Rectangle bounds,
        bool value,
        bool enabled,
        Vector2 mousePosition,
        bool mousePressed)
    {
        bool isHovered = UiHitTesting.Contains(bounds, mousePosition);
        Color fill = enabled
            ? (value ? UiTheme.AccentSoftColor : (isHovered ? UiTheme.PanelHoverColor : UiTheme.PanelColor))
            : UiTheme.DisabledFillColor;
        Color outline = enabled
            ? (value ? UiTheme.AccentColor : UiTheme.BorderColor)
            : UiTheme.DisabledBorderColor;

        Graphics.DrawRectangleRec(bounds, fill);
        Graphics.DrawRectangleLinesEx(bounds, value ? 2f : 1f, outline);

        if (value)
        {
            Graphics.DrawLineEx(new Vector2(bounds.X + 4, bounds.Y + (bounds.Height * 0.55f)), new Vector2(bounds.X + 8, bounds.Y + bounds.Height - 5), 2f, UiTheme.AccentColor);
            Graphics.DrawLineEx(new Vector2(bounds.X + 8, bounds.Y + bounds.Height - 5), new Vector2(bounds.X + bounds.Width - 4, bounds.Y + 4), 2f, UiTheme.AccentColor);
        }

        if (enabled && mousePressed && isHovered)
        {
            value = !value;
        }

        return value;
    }

    public static float DrawCompactSlider(
        UiControlId index,
        ref UiControlId? activeSlider,
        bool enabled,
        Rectangle bounds,
        float value,
        float minValue,
        float maxValue,
        Vector2 mousePosition,
        bool mousePressed,
        bool mouseDown)
    {
        float knobSize = MathF.Min(bounds.Width, 24f);
        Rectangle knobBounds = new(bounds.X + ((bounds.Width - knobSize) / 2f), bounds.Y - 6f, knobSize, knobSize);
        Rectangle dragBounds = new(bounds.X, knobBounds.Y - 8f, bounds.Width, knobBounds.Height + 16f);
        Color effectivePanelColor = enabled ? UiTheme.PanelColor : UiTheme.DisabledPanelColor;
        Color effectiveBorderColor = enabled ? UiTheme.BorderColor : UiTheme.DisabledBorderColor;
        Color effectiveAccentSoftColor = enabled ? UiTheme.AccentSoftColor : UiTheme.DisabledAccentSoftColor;
        Color effectiveAccentColor = enabled ? UiTheme.AccentColor : UiTheme.DisabledAccentColor;

        if (enabled && mousePressed && UiHitTesting.Contains(knobBounds, mousePosition))
        {
            activeSlider = index;
        }

        if (enabled && mouseDown && activeSlider == index)
        {
            float normalized = GetKnobDragRatio(mousePosition, knobBounds, dragBounds);
            value = minValue + ((maxValue - minValue) * normalized);
        }

        float ratio = (value - minValue) / (maxValue - minValue);
        DrawKnob(knobBounds, ratio, effectivePanelColor, effectiveBorderColor, effectiveAccentSoftColor, effectiveAccentColor);

        return Math.Clamp(value, minValue, maxValue);
    }

    public static void DrawWaveformScope(Rectangle bounds, float[] samples, int writeIndex)
    {
        Graphics.DrawText("Output waveform", (int)bounds.X + 18, (int)bounds.Y + 16, 22, UiTheme.MutedTextColor);

        Rectangle graphBounds = new(bounds.X + 18, bounds.Y + 52, bounds.Width - 36, bounds.Height - 70);
        Graphics.DrawRectangleRec(graphBounds, UiTheme.AnalysisSurfaceColor);
        Graphics.DrawRectangleLinesEx(graphBounds, 1f, UiTheme.BorderColor);

        Vector2 centerStart = new(graphBounds.X, graphBounds.Y + (graphBounds.Height / 2f));
        Vector2 centerEnd = new(graphBounds.X + graphBounds.Width, graphBounds.Y + (graphBounds.Height / 2f));
        Graphics.DrawLineV(centerStart, centerEnd, UiTheme.ScopeCenterLineColor);

        int sampleCount = samples.Length;
        float xStep = graphBounds.Width / (sampleCount - 1);
        float centerY = graphBounds.Y + (graphBounds.Height / 2f);
        float amplitude = graphBounds.Height * 0.42f;

        Vector2 previous = new(graphBounds.X, centerY - (samples[writeIndex] * amplitude));

        for (int i = 1; i < sampleCount; i++)
        {
            int sampleIndex = (writeIndex + i) % sampleCount;
            Vector2 current = new(graphBounds.X + (xStep * i), centerY - (samples[sampleIndex] * amplitude));
            Graphics.DrawLineV(previous, current, UiTheme.AccentStrongColor);
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
        float resonance)
    {
        Graphics.DrawText("Filter analysis", (int)bounds.X + 18, (int)bounds.Y + 16, 22, UiTheme.MutedTextColor);
        Graphics.DrawText("Recent output spectrum", (int)bounds.X + 18, (int)bounds.Y + 42, 18, UiTheme.MutedTextColor);

        Rectangle graphBounds = new(bounds.X + 18, bounds.Y + 76, bounds.Width - 36, bounds.Height - 94);
        Graphics.DrawRectangleRec(graphBounds, UiTheme.AnalysisSurfaceColor);
        Graphics.DrawRectangleLinesEx(graphBounds, 1f, UiTheme.BorderColor);

        DrawFilterGrid(graphBounds, UiTheme.BorderColor, UiTheme.MutedTextColor);
        DrawSpectrumCurve(graphBounds, samples, writeIndex, sampleRate, UiTheme.AccentStrongColor);
        DrawFilterResponseCurve(graphBounds, sampleRate, filterType, cutoffHz, resonance, UiTheme.AccentColor);
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
                    UiTheme.AnalysisGridColor);
            Graphics.DrawText(frequencyLabels[i], (int)x - 14, (int)graphBounds.Y + (int)graphBounds.Height + 6, 16, labelColor);
        }

        for (int i = 1; i < 4; i++)
        {
            float y = graphBounds.Y + ((graphBounds.Height / 4f) * i);
            Graphics.DrawLineV(
                new Vector2(graphBounds.X, y),
                new Vector2(graphBounds.X + graphBounds.Width, y),
                UiTheme.AnalysisGridColor);
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
        int hoveredNote)
    {
        foreach (PianoKeyLayout key in keys.Where(static key => !key.IsBlack))
        {
            bool isActive = activeNotes.Contains(key.MidiNote);
            bool isHovered = key.MidiNote == hoveredNote;
            Color fill = isActive ? UiTheme.AccentSoftColor : (isHovered ? UiTheme.WhiteKeyHoverColor : UiTheme.WhiteKeyColor);

            Graphics.DrawRectangleRec(key.Bounds, fill);
            Graphics.DrawRectangleLinesEx(key.Bounds, 1f, UiTheme.BorderColor);

            if (key.MidiNote % 12 == 0 || key.MidiNote == 21)
            {
                const int labelFontSize = 14;
                int labelWidth = TextManager.MeasureText(key.Label, labelFontSize);
                int labelX = (int)(key.Bounds.X + ((key.Bounds.Width - labelWidth) / 2f));
                int labelY = (int)(key.Bounds.Y + key.Bounds.Height - 22);
                Graphics.DrawText(key.Label, labelX, labelY, labelFontSize, UiTheme.TextColor);
            }
        }

        foreach (PianoKeyLayout key in keys.Where(static key => key.IsBlack))
        {
            bool isActive = activeNotes.Contains(key.MidiNote);
            bool isHovered = key.MidiNote == hoveredNote;
            Color fill = isActive ? UiTheme.AccentStrongColor : (isHovered ? UiTheme.DarkKeyHoverColor : UiTheme.DarkKeyColor);

            Graphics.DrawRectangleRec(key.Bounds, fill);
            Graphics.DrawRectangleLinesEx(key.Bounds, 1f, UiTheme.DarkKeyBorderColor);
        }
    }

    public static void DrawPanel(Rectangle bounds, Color fillColor)
    {
        Graphics.DrawRectangleRec(bounds, fillColor);
        Graphics.DrawRectangleLinesEx(bounds, 1f, UiTheme.BorderColor);
    }

    private static TEnum DrawEnumButtons<TEnum>(
        Rectangle area,
        ReadOnlySpan<TEnum> values,
        TEnum currentValue,
        Vector2 mousePosition,
        bool mousePressed,
        Func<TEnum, string> labelSelector)
        where TEnum : struct, Enum
    {
        float buttonGap = 10f;
        float buttonWidth = (area.Width - (buttonGap * (values.Length - 1))) / values.Length;

        for (int i = 0; i < values.Length; i++)
        {
            Rectangle buttonBounds = new(area.X + (i * (buttonWidth + buttonGap)), area.Y, buttonWidth, area.Height);
            bool isSelected = EqualityComparer<TEnum>.Default.Equals(currentValue, values[i]);
            bool isHovered = UiHitTesting.Contains(buttonBounds, mousePosition);
            string buttonLabel = labelSelector(values[i]);
            const int buttonFontSize = 18;
            Color fill = isSelected ? UiTheme.AccentSoftColor : (isHovered ? UiTheme.PanelHoverColor : UiTheme.PanelColor);
            Color outline = isSelected ? UiTheme.AccentStrongColor : UiTheme.BorderColor;

            Graphics.DrawRectangleRec(buttonBounds, fill);
            Graphics.DrawRectangleLinesEx(buttonBounds, isSelected ? 2.5f : 1f, outline);
            int textWidth = TextManager.MeasureText(buttonLabel, buttonFontSize);
            int textX = (int)(buttonBounds.X + ((buttonBounds.Width - textWidth) / 2f));
            int textY = (int)(buttonBounds.Y + ((buttonBounds.Height - buttonFontSize) / 2f) - 1f);
            Graphics.DrawText(buttonLabel, textX, textY, buttonFontSize, UiTheme.TextColor);

            if (mousePressed && isHovered)
            {
                currentValue = values[i];
            }
        }

        return currentValue;
    }

    private static int DrawIndexedCycler(
        Rectangle area,
        int currentIndex,
        bool enabled,
        Vector2 mousePosition,
        bool mousePressed,
        IReadOnlyList<string> labels)
    {
        currentIndex = Math.Clamp(currentIndex, 0, labels.Count - 1);

        const float arrowWidth = 26f;
        Rectangle previousBounds = new(area.X, area.Y, arrowWidth, area.Height);
        Rectangle nextBounds = new(area.X + area.Width - arrowWidth, area.Y, arrowWidth, area.Height);
        Rectangle valueBounds = new(area.X + arrowWidth + 6f, area.Y, area.Width - ((arrowWidth * 2f) + 12f), area.Height);
        bool isPreviousHovered = UiHitTesting.Contains(previousBounds, mousePosition);
        bool isNextHovered = UiHitTesting.Contains(nextBounds, mousePosition);
        bool canInteract = enabled && mousePressed;

        if (canInteract && isPreviousHovered)
        {
            currentIndex = (currentIndex - 1 + labels.Count) % labels.Count;
        }
        else if (canInteract && isNextHovered)
        {
            currentIndex = (currentIndex + 1) % labels.Count;
        }

        Color inactiveFill = UiTheme.DisabledFillColor;
        Color inactiveOutline = UiTheme.DisabledBorderColor;
        Color inactiveText = UiTheme.DisabledTextColor;
        Color effectivePanelColor = enabled ? UiTheme.PanelColor : inactiveFill;
        Color effectiveBorderColor = enabled ? UiTheme.BorderColor : inactiveOutline;
        Color effectiveTextColor = enabled ? UiTheme.TextColor : inactiveText;

        DrawCyclerButton(previousBounds, "<", enabled, isPreviousHovered, effectiveTextColor);
        Graphics.DrawRectangleRec(valueBounds, enabled ? UiTheme.AccentSoftColor : effectivePanelColor);
        Graphics.DrawRectangleLinesEx(valueBounds, 1f, enabled ? UiTheme.AccentStrongColor : effectiveBorderColor);
        DrawCenteredWrappedLabel(valueBounds, labels[currentIndex], 16, effectiveTextColor);
        DrawCyclerButton(nextBounds, ">", enabled, isNextHovered, effectiveTextColor);

        return currentIndex;
    }

    private static TEnum DrawEnumCycler<TEnum>(
        Rectangle area,
        TEnum currentValue,
        bool enabled,
        Vector2 mousePosition,
        bool mousePressed,
        Func<TEnum, string> labelSelector)
        where TEnum : struct, Enum
    {
        TEnum[] values = Enum.GetValues<TEnum>();
        int currentIndex = Array.IndexOf(values, currentValue);
        currentIndex = Math.Max(0, currentIndex);

        const float arrowWidth = 26f;
        Rectangle previousBounds = new(area.X, area.Y, arrowWidth, area.Height);
        Rectangle nextBounds = new(area.X + area.Width - arrowWidth, area.Y, arrowWidth, area.Height);
        Rectangle valueBounds = new(area.X + arrowWidth + 6f, area.Y, area.Width - ((arrowWidth * 2f) + 12f), area.Height);
        bool isPreviousHovered = UiHitTesting.Contains(previousBounds, mousePosition);
        bool isNextHovered = UiHitTesting.Contains(nextBounds, mousePosition);
        bool canInteract = enabled && mousePressed;

        if (canInteract && isPreviousHovered)
        {
            currentIndex = (currentIndex - 1 + values.Length) % values.Length;
            currentValue = values[currentIndex];
        }
        else if (canInteract && isNextHovered)
        {
            currentIndex = (currentIndex + 1) % values.Length;
            currentValue = values[currentIndex];
        }

        Color inactiveFill = UiTheme.DisabledFillColor;
        Color inactiveOutline = UiTheme.DisabledBorderColor;
        Color inactiveText = UiTheme.DisabledTextColor;
        Color effectivePanelColor = enabled ? UiTheme.PanelColor : inactiveFill;
        Color effectiveBorderColor = enabled ? UiTheme.BorderColor : inactiveOutline;
        Color effectiveTextColor = enabled ? UiTheme.TextColor : inactiveText;

        DrawCyclerButton(previousBounds, "<", enabled, isPreviousHovered, effectiveTextColor);
        Graphics.DrawRectangleRec(valueBounds, enabled ? UiTheme.AccentSoftColor : effectivePanelColor);
        Graphics.DrawRectangleLinesEx(valueBounds, 1f, enabled ? UiTheme.AccentStrongColor : effectiveBorderColor);
        DrawCenteredWrappedLabel(valueBounds, labelSelector(currentValue), 16, effectiveTextColor);
        DrawCyclerButton(nextBounds, ">", enabled, isNextHovered, effectiveTextColor);

        return currentValue;
    }

    private static void DrawCyclerButton(
        Rectangle bounds,
        string label,
        bool enabled,
        bool isHovered,
        Color textColor)
    {
        Color fill = enabled
            ? (isHovered ? UiTheme.AccentSoftColor : UiTheme.PanelColor)
            : UiTheme.DisabledFillColor;
        Color outline = enabled
            ? (isHovered ? UiTheme.AccentStrongColor : UiTheme.BorderColor)
            : UiTheme.DisabledBorderColor;

        Graphics.DrawRectangleRec(bounds, fill);
        Graphics.DrawRectangleLinesEx(bounds, 1f, outline);
        int textWidth = TextManager.MeasureText(label, 18);
        int textX = (int)(bounds.X + ((bounds.Width - textWidth) / 2f));
        int textY = (int)(bounds.Y + ((bounds.Height - 18f) / 2f) - 1f);
        Graphics.DrawText(label, textX, textY, 18, textColor);
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

    private static void DrawWaveformPreview(Rectangle bounds, Waveform waveform, Color waveColor, Color borderColor)
    {
        Rectangle previewBounds = new(bounds.X + 8f, bounds.Y + 8f, bounds.Width - 16f, bounds.Height - 16f);
        float centerY = previewBounds.Y + (previewBounds.Height / 2f);
        Graphics.DrawLineEx(new Vector2(previewBounds.X, centerY), new Vector2(previewBounds.X + previewBounds.Width, centerY), 1f, UiTheme.PreviewGuideColor);

        switch (waveform)
        {
            case Waveform.Noise:
            case Waveform.PinkNoise:
                DrawNoisePreview(previewBounds, waveColor, waveform == Waveform.PinkNoise);
                return;
            case Waveform.Organ:
                DrawOrganPreview(previewBounds, waveColor);
                return;
            case Waveform.SuperSaw:
                DrawSuperSawPreview(previewBounds, waveColor);
                return;
            case Waveform.Metallic:
                DrawMetallicPreview(previewBounds, waveColor);
                return;
        }

        const int pointCount = 28;
        Vector2 previousPoint = new(previewBounds.X, GetWaveformPreviewY(waveform, 0f, previewBounds));
        for (int i = 1; i < pointCount; i++)
        {
            float t = i / (pointCount - 1f);
            Vector2 point = new(previewBounds.X + (previewBounds.Width * t), GetWaveformPreviewY(waveform, t, previewBounds));
            Graphics.DrawLineEx(previousPoint, point, 2f, waveColor);
            previousPoint = point;
        }
    }

    private static float GetWaveformPreviewY(Waveform waveform, float t, Rectangle bounds)
    {
        float amplitude = bounds.Height * 0.38f;
        float centerY = bounds.Y + (bounds.Height / 2f);
        float phase = t * MathF.Tau;
        float sample = waveform switch
        {
            Waveform.Sine => MathF.Sin(phase),
            Waveform.Square => MathF.Sin(phase) >= 0f ? 1f : -1f,
            Waveform.Pulse => (t % 1f) < 0.22f ? 1f : -1f,
            Waveform.Saw => 1f - (2f * t),
            Waveform.Triangle => 1f - (4f * MathF.Abs(t - 0.5f)),
            _ => MathF.Sin(phase)
        };

        return centerY - (sample * amplitude);
    }

    private static void DrawNoisePreview(Rectangle bounds, Color waveColor, bool pink)
    {
        const int pointCount = 24;
        Vector2 previousPoint = new(bounds.X, bounds.Y + (bounds.Height * 0.55f));
        for (int i = 1; i < pointCount; i++)
        {
            float t = i / (pointCount - 1f);
            float baseWave = MathF.Sin((t * MathF.Tau * (pink ? 2.5f : 6f)) + (pink ? 0.4f : 1.3f));
            float detail = MathF.Sin((t * MathF.Tau * (pink ? 6f : 13f)) + (pink ? 1.2f : 0.2f));
            float sample = pink ? ((baseWave * 0.7f) + (detail * 0.25f)) : ((baseWave * 0.45f) + (detail * 0.55f));
            Vector2 point = new(bounds.X + (bounds.Width * t), bounds.Y + (bounds.Height * 0.5f) - (sample * bounds.Height * (pink ? 0.28f : 0.36f)));
            Graphics.DrawLineEx(previousPoint, point, 2f, waveColor);
            previousPoint = point;
        }
    }

    private static void DrawOrganPreview(Rectangle bounds, Color waveColor)
    {
        float x = bounds.X + 6f;
        float width = 6f;
        float gap = 4f;
        float[] heights = [0.35f, 0.58f, 0.82f, 0.62f, 0.42f];
        foreach (float height in heights)
        {
            Rectangle bar = new(x, bounds.Y + bounds.Height - (bounds.Height * height), width, bounds.Height * height);
            Graphics.DrawRectangleRec(bar, waveColor);
            x += width + gap;
        }
    }

    private static void DrawSuperSawPreview(Rectangle bounds, Color waveColor)
    {
        for (int i = 0; i < 3; i++)
        {
            float offset = i * 4f;
            Vector2 left = new(bounds.X + offset, bounds.Y + bounds.Height * 0.2f);
            Vector2 peak = new(bounds.X + (bounds.Width * 0.45f) + offset, bounds.Y + bounds.Height * 0.8f);
            Vector2 right = new(bounds.X + bounds.Width - 4f + offset, bounds.Y + bounds.Height * 0.2f);
            Graphics.DrawLineEx(left, peak, 2f, waveColor);
            Graphics.DrawLineEx(peak, right, 2f, waveColor);
        }
    }

    private static void DrawMetallicPreview(Rectangle bounds, Color waveColor)
    {
        const int pointCount = 24;
        Vector2 previousPoint = new(bounds.X, bounds.Y + (bounds.Height * 0.5f));
        for (int i = 1; i < pointCount; i++)
        {
            float t = i / (pointCount - 1f);
            float sample = (MathF.Sin(t * MathF.Tau * 3f) * 0.6f) + (MathF.Sin(t * MathF.Tau * 9f) * 0.35f);
            Vector2 point = new(bounds.X + (bounds.Width * t), bounds.Y + (bounds.Height * 0.5f) - (sample * bounds.Height * 0.34f));
            Graphics.DrawLineEx(previousPoint, point, 2f, waveColor);
            previousPoint = point;
        }
    }

    private static void DrawKnob(Rectangle bounds, float ratio, Color panelColor, Color borderColor, Color accentSoftColor, Color accentColor)
    {
        Vector2 center = new(bounds.X + (bounds.Width / 2f), bounds.Y + (bounds.Height / 2f));
        float radius = MathF.Min(bounds.Width, bounds.Height) * 0.5f;
        float indicatorLength = radius - 5f;
        float angle = (-225f + (270f * Math.Clamp(ratio, 0f, 1f))) * (MathF.PI / 180f);
        Vector2 indicatorEnd = new(center.X + (MathF.Cos(angle) * indicatorLength), center.Y + (MathF.Sin(angle) * indicatorLength));

        Graphics.DrawCircleV(center, radius, panelColor);
        Graphics.DrawCircleLines((int)center.X, (int)center.Y, radius, borderColor);
        Graphics.DrawRing(center, radius - 4f, radius - 1f, -225f, -225f + (270f * Math.Clamp(ratio, 0f, 1f)), 32, accentSoftColor);
        Graphics.DrawLineEx(center, indicatorEnd, 2.5f, accentColor);
        Graphics.DrawCircleV(center, 3.5f, accentColor);
    }

    private static float GetKnobDragRatio(Vector2 mousePosition, Rectangle knobBounds, Rectangle dragBounds)
    {
        Vector2 center = new(knobBounds.X + (knobBounds.Width / 2f), knobBounds.Y + (knobBounds.Height / 2f));
        float dx = mousePosition.X - center.X;
        float dy = center.Y - mousePosition.Y;
        float horizontalRange = MathF.Max(dragBounds.Width * 0.5f, 1f);
        float verticalRange = MathF.Max(dragBounds.Height * 0.5f, 1f);
        float dominantDelta = MathF.Abs(dx) >= MathF.Abs(dy)
            ? dx / horizontalRange
            : dy / verticalRange;

        return (Math.Clamp(dominantDelta, -1f, 1f) + 1f) * 0.5f;
    }
}
