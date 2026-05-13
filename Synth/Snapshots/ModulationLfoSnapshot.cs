namespace TinySynth.Synth.Snapshots;

internal readonly record struct ModulationLfoSnapshot(
    ModulationLfoShape Shape,
    float RateHz,
    float Depth)
{
    public static ModulationLfoSnapshot Create(ModulationLfoParameters source)
    {
        return new ModulationLfoSnapshot(source.Shape, source.RateHz, source.Depth);
    }
}
