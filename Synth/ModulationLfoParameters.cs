namespace TinySynth.Synth;

internal sealed class ModulationLfoParameters
{
    public ModulationLfoShape Shape { get; set; } = ModulationLfoShape.Sine;

    public float RateHz { get; set; } = 2.5f;

    public float Depth { get; set; } = 1f;

    public void ResetToDefaults()
    {
        Shape = ModulationLfoShape.Sine;
        RateHz = 2.5f;
        Depth = 1f;
    }
}
