namespace TinySynth.Synth;

internal sealed class SynthParameters
{
    public Waveform Waveform { get; set; } = Waveform.Sine;

    public float Gain { get; set; } = 0.80f;

    public float DetuneCents { get; set; } = 0f;

    public float GlideSeconds { get; set; } = 0f;

    public float VibratoDepthCents { get; set; } = 0f;

    public float VibratoRateHz { get; set; } = 5f;

    public float AttackSeconds { get; set; } = 0.05f;

    public float DecaySeconds { get; set; } = 0.18f;

    public float SustainLevel { get; set; } = 0.72f;

    public float ReleaseSeconds { get; set; } = 0.30f;
}
