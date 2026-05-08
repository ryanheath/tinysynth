using Raylib_CSharp.Rendering;
using Raylib_CSharp.Transformations;
using System.Numerics;
using TinySynth.Synth;
using TinySynth.UI;

namespace TinySynth.App;

internal readonly record struct PresetSectionResult(
    int ActivePresetFamilyIndex,
    int ActivePresetIndex,
    bool PresetApplied);

internal static class PresetSectionController
{
    public static PresetSectionResult Draw(
        Rectangle controlPanel,
        LayoutMetrics layout,
        int activePresetFamilyIndex,
        int activePresetIndex,
        Vector2 mousePosition,
        bool mousePressed,
        SynthParameters synthParameters)
    {
        Graphics.DrawText("GM presets", (int)controlPanel.X + 20, (int)controlPanel.Y + 112, 18, UiTheme.MutedTextColor);

        IReadOnlyList<GmInstrumentFamily> presetFamilies = GmPresetCatalog.Families;
        int previousFamilyIndex = activePresetFamilyIndex;
        activePresetFamilyIndex = SynthRenderer.DrawPresetFamilyButtons(
            layout.PresetFamilyArea,
            presetFamilies,
            activePresetFamilyIndex,
            mousePosition,
            mousePressed);

        if (previousFamilyIndex != activePresetFamilyIndex)
        {
            activePresetIndex = 0;
        }

        IReadOnlyList<GmPreset> visiblePresets = GmPresetCatalog.GetPresets(presetFamilies[activePresetFamilyIndex]);
        int previousPresetIndex = activePresetIndex;
        activePresetIndex = SynthRenderer.DrawPresetButtons(
            layout.PresetOptionArea,
            visiblePresets,
            activePresetIndex,
            mousePosition,
            mousePressed);

        bool presetApplied = (previousPresetIndex != activePresetIndex || previousFamilyIndex != activePresetFamilyIndex)
            && visiblePresets.Count > 0;

        if (presetApplied)
        {
            GmPresetCatalog.ApplyPreset(visiblePresets[activePresetIndex], synthParameters);
        }

        return new PresetSectionResult(activePresetFamilyIndex, activePresetIndex, presetApplied);
    }
}
