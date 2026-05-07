namespace TinySynth.Synth;

internal sealed class SynthParameters
{
    public Waveform Waveform { get; set; } = Waveform.Sine;

    public float AttackSeconds { get; set; } = 0.05f;

    public float DecaySeconds { get; set; } = 0.18f;

    public float SustainLevel { get; set; } = 0.72f;

    public float ReleaseSeconds { get; set; } = 0.30f;
}
