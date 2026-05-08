namespace TinySynth.Synth;

internal sealed class SynthParameters
{
    public const int OscillatorCount = 4;

    private readonly OscillatorParameters[] _oscillators =
    [
        new(),
        new(),
        new(),
        new()
    ];

    public IReadOnlyList<OscillatorParameters> Oscillators => _oscillators;

    public FilterType FilterType { get; set; } = FilterType.Off;

    public float FilterCutoffHz { get; set; } = 12_000f;

    public float FilterResonance { get; set; } = 0.15f;

    public float FilterEnvelopeAmount { get; set; } = 0f;

    public float FilterLfoDepth { get; set; } = 0f;

    public float FilterLfoRateHz { get; set; } = 2.5f;

    public ChorusType ChorusType { get; set; } = ChorusType.Off;

    public float ChorusMix { get; set; } = 0f;

    public float ChorusRateHz { get; set; } = 0.8f;

    public float ChorusDepth { get; set; } = 0.35f;

    public ReverbType ReverbType { get; set; } = ReverbType.Off;

    public float ReverbMix { get; set; } = 0f;

    public float ReverbSize { get; set; } = 0.45f;

    public float ReverbDamping { get; set; } = 0.35f;

    public DelayType DelayType { get; set; } = DelayType.Off;

    public float DelayMix { get; set; } = 0f;

    public float DelayTimeSeconds { get; set; } = 0.28f;

    public float DelayFeedback { get; set; } = 0.30f;

    public OscillatorParameters GetOscillator(int index)
    {
        return _oscillators[Math.Clamp(index, 0, _oscillators.Length - 1)];
    }
}

internal sealed class OscillatorParameters
{
    public bool Enabled { get; set; } = true;

    public Waveform Waveform { get; set; } = Waveform.Sine;

    public float Gain { get; set; } = 0.80f;

    public float DetuneCents { get; set; } = 0f;

    public float GlideSeconds { get; set; } = 0f;

    public float VibratoDepthCents { get; set; } = 0f;

    public float VibratoRateHz { get; set; } = 5f;

    public float PulseWidth { get; set; } = 0.50f;

    public float PwmRateHz { get; set; } = 0f;

    public float AttackSeconds { get; set; } = 0.05f;

    public float DecaySeconds { get; set; } = 0.18f;

    public float SustainLevel { get; set; } = 0.72f;

    public float ReleaseSeconds { get; set; } = 0.30f;
}
