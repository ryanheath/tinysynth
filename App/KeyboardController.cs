using Raylib_CSharp.Rendering;
using Raylib_CSharp.Transformations;
using System.Numerics;
using TinySynth.UI;

namespace TinySynth.App;

internal static class KeyboardController
{
    public static float Draw(
        Rectangle keyboardPanel,
        Rectangle holdPedalBounds,
        Rectangle masterVolumeBounds,
        IReadOnlySet<int> activeNotes,
        int hoveredMidiNote,
        bool holdPedalEnabled,
        PianoKeyLayout[] keys,
        ref int activeSlider,
        float masterVolume,
        Vector2 mousePosition,
        bool mousePressed,
        bool mouseDown)
    {
        Graphics.DrawText("Keyboard", (int)keyboardPanel.X + 18, (int)keyboardPanel.Y + 14, 22, UiTheme.TextColor);

        float newMasterVolume = SynthRenderer.DrawSlider(
            index: 9,
            activeSlider: ref activeSlider,
            enabled: true,
            label: "Master",
            valueLabel: $"{(masterVolume * 100):0}%",
            bounds: masterVolumeBounds,
            value: masterVolume,
            minValue: 0.00f,
            maxValue: 1.00f,
            mousePosition: mousePosition,
            mousePressed: mousePressed,
            mouseDown: mouseDown);

        SynthRenderer.DrawToggle(
            holdPedalBounds,
            "Hold pedal",
            holdPedalEnabled);

        SynthRenderer.DrawKeyboard(
            keys,
            activeNotes,
            hoveredMidiNote);

        return newMasterVolume;
    }
}
