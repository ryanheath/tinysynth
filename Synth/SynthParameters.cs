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

    public float ChorusTremoloDepth { get; set; } = 0f;

    public ReverbType ReverbType { get; set; } = ReverbType.Off;

    public float ReverbMix { get; set; } = 0f;

    public float ReverbSize { get; set; } = 0.45f;

    public float ReverbDamping { get; set; } = 0.35f;

    public DelayType DelayType { get; set; } = DelayType.Off;

    public float DelayMix { get; set; } = 0f;

    public float DelayTimeSeconds { get; set; } = 0.28f;

    public float DelayFeedback { get; set; } = 0.30f;

    public void ResetToDefaults()
    {
        FilterType = FilterType.Off;
        FilterCutoffHz = 12_000f;
        FilterResonance = 0.15f;
        FilterEnvelopeAmount = 0f;
        FilterLfoDepth = 0f;
        FilterLfoRateHz = 2.5f;

        ChorusType = ChorusType.Off;
        ChorusMix = 0f;
        ChorusRateHz = 0.8f;
        ChorusDepth = 0.35f;
        ChorusTremoloDepth = 0f;

        ReverbType = ReverbType.Off;
        ReverbMix = 0f;
        ReverbSize = 0.45f;
        ReverbDamping = 0.35f;

        DelayType = DelayType.Off;
        DelayMix = 0f;
        DelayTimeSeconds = 0.28f;
        DelayFeedback = 0.30f;

        foreach (OscillatorParameters oscillator in _oscillators)
        {
            oscillator.ResetToDefaults();
        }
    }

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

    public float Pan { get; set; } = 0f;

    public float AttackSeconds { get; set; } = 0.05f;

    public float DecaySeconds { get; set; } = 0.18f;

    public float SustainLevel { get; set; } = 0.72f;

    public float ReleaseSeconds { get; set; } = 0.30f;

    public void ResetToDefaults()
    {
        Enabled = true;
        Waveform = Waveform.Sine;
        Gain = 0.80f;
        DetuneCents = 0f;
        GlideSeconds = 0f;
        VibratoDepthCents = 0f;
        VibratoRateHz = 5f;
        PulseWidth = 0.50f;
        PwmRateHz = 0f;
        Pan = 0f;
        AttackSeconds = 0.05f;
        DecaySeconds = 0.18f;
        SustainLevel = 0.72f;
        ReleaseSeconds = 0.30f;
    }
}
