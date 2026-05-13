namespace TinySynth.Synth.Snapshots;

internal readonly record struct OscillatorSnapshot(
    bool Enabled,
    Waveform Waveform,
    float Gain,
    float DetuneCents,
    float GlideSeconds,
    float VibratoDepthCents,
    float VibratoRateHz,
    float PulseWidth,
    float PwmRateHz,
    float Pan,
    EnvelopeMode EnvelopeMode,
    float AttackSeconds,
    float DecaySeconds,
    float SustainLevel,
    float ReleaseSeconds)
{
    public static OscillatorSnapshot Create(OscillatorParameters source)
    {
        return new OscillatorSnapshot(
            source.Enabled,
            source.Waveform,
            source.Gain,
            source.DetuneCents,
            source.GlideSeconds,
            source.VibratoDepthCents,
            source.VibratoRateHz,
            source.PulseWidth,
            source.PwmRateHz,
            source.Pan,
            source.EnvelopeMode,
            source.AttackSeconds,
            source.DecaySeconds,
            source.SustainLevel,
            source.ReleaseSeconds);
    }
}
