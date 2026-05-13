namespace TinySynth.Synth.Modulation;

internal readonly record struct GlobalModulationState(
    float ChorusMix,
    float DelayMix,
    float ReverbMix)
{
    public static GlobalModulationState Empty { get; } = new();

    public float GetValue(ModulationDestination destination)
    {
        return destination switch
        {
            ModulationDestination.ChorusMix => ChorusMix,
            ModulationDestination.DelayMix => DelayMix,
            ModulationDestination.ReverbMix => ReverbMix,
            _ => 0f
        };
    }
}
