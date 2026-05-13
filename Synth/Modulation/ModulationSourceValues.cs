namespace TinySynth.Synth.Modulation;

internal readonly record struct ModulationSourceValues(
    float Lfo1,
    float Lfo2,
    float Envelope,
    float KeyTrack)
{
    public float GetValue(ModulationSource source)
    {
        return source switch
        {
            ModulationSource.Lfo1 => Lfo1,
            ModulationSource.Lfo2 => Lfo2,
            ModulationSource.Envelope => Envelope,
            ModulationSource.KeyTrack => KeyTrack,
            _ => 0f
        };
    }
}
