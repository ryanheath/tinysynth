namespace TinySynth.Synth.Modulation;

internal readonly record struct VoiceModulationState(
    float Pitch,
    float FilterCutoff,
    float FilterResonance,
    float Gain,
    float Pan,
    float PulseWidth,
    float Lfo1Rate,
    float Lfo2Rate)
{
    public static VoiceModulationState Empty { get; } = new();

    public float GetValue(ModulationDestination destination)
    {
        return destination switch
        {
            ModulationDestination.Pitch => Pitch,
            ModulationDestination.FilterCutoff => FilterCutoff,
            ModulationDestination.FilterResonance => FilterResonance,
            ModulationDestination.Gain => Gain,
            ModulationDestination.Pan => Pan,
            ModulationDestination.PulseWidth => PulseWidth,
            ModulationDestination.Lfo1Rate => Lfo1Rate,
            ModulationDestination.Lfo2Rate => Lfo2Rate,
            _ => 0f
        };
    }
}
