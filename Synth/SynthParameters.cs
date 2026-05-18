namespace TinySynth.Synth;

internal sealed class SynthParameters
{
    public const int OscillatorCount = 4;
    public const int ModulationRouteCount = 6;

    private const float DefaultFilterCutoffHz = 12_000f;
    private const float DefaultFilterResonance = 0.15f;
    private const float DefaultFilterEnvelopeAmount = 0f;
    private const float DefaultFilterLfoDepth = 0f;
    private const float DefaultFilterLfoRateHz = 2.5f;
    private const float DefaultChorusMix = 0f;
    private const float DefaultChorusRateHz = 0.8f;
    private const float DefaultChorusDepth = 0.35f;
    private const float DefaultChorusTremoloDepth = 0f;
    private const float DefaultReverbMix = 0f;
    private const float DefaultReverbSize = 0.45f;
    private const float DefaultReverbDamping = 0.35f;
    private const float DefaultDelayMix = 0f;
    private const float DefaultDelayTimeSeconds = 0.28f;
    private const float DefaultDelayFeedback = 0.30f;

    private readonly OscillatorParameters[] _oscillators =
    [
        new(),
        new(),
        new(),
        new()
    ];

    private readonly ModulationRoute[] _modulationRoutes =
    [
        new(),
        new(),
        new(),
        new(),
        new(),
        new()
    ];

    public IReadOnlyList<OscillatorParameters> Oscillators => _oscillators;

    public IReadOnlyList<ModulationRoute> ModulationRoutes => _modulationRoutes;

    public ModulationLfoParameters Lfo1 { get; } = new();

    public ModulationLfoParameters Lfo2 { get; } = new();

    public FilterType FilterType { get; set; } = FilterType.Off;

    public float FilterCutoffHz { get; set; } = DefaultFilterCutoffHz;

    public float FilterResonance { get; set; } = DefaultFilterResonance;

    public float FilterEnvelopeAmount { get; set; } = DefaultFilterEnvelopeAmount;

    public float FilterLfoDepth { get; set; } = DefaultFilterLfoDepth;

    public float FilterLfoRateHz { get; set; } = DefaultFilterLfoRateHz;

    public ChorusType ChorusType { get; set; } = ChorusType.Off;

    public float ChorusMix { get; set; } = DefaultChorusMix;

    public float ChorusRateHz { get; set; } = DefaultChorusRateHz;

    public float ChorusDepth { get; set; } = DefaultChorusDepth;

    public float ChorusTremoloDepth { get; set; } = DefaultChorusTremoloDepth;

    public ReverbType ReverbType { get; set; } = ReverbType.Off;

    public float ReverbMix { get; set; } = DefaultReverbMix;

    public float ReverbSize { get; set; } = DefaultReverbSize;

    public float ReverbDamping { get; set; } = DefaultReverbDamping;

    public DelayType DelayType { get; set; } = DelayType.Off;

    public float DelayMix { get; set; } = DefaultDelayMix;

    public float DelayTimeSeconds { get; set; } = DefaultDelayTimeSeconds;

    public float DelayFeedback { get; set; } = DefaultDelayFeedback;

    public void ResetToDefaults()
    {
        FilterType = FilterType.Off;
        FilterCutoffHz = DefaultFilterCutoffHz;
        FilterResonance = DefaultFilterResonance;
        FilterEnvelopeAmount = DefaultFilterEnvelopeAmount;
        FilterLfoDepth = DefaultFilterLfoDepth;
        FilterLfoRateHz = DefaultFilterLfoRateHz;

        ChorusType = ChorusType.Off;
        ChorusMix = DefaultChorusMix;
        ChorusRateHz = DefaultChorusRateHz;
        ChorusDepth = DefaultChorusDepth;
        ChorusTremoloDepth = DefaultChorusTremoloDepth;

        ReverbType = ReverbType.Off;
        ReverbMix = DefaultReverbMix;
        ReverbSize = DefaultReverbSize;
        ReverbDamping = DefaultReverbDamping;

        DelayType = DelayType.Off;
        DelayMix = DefaultDelayMix;
        DelayTimeSeconds = DefaultDelayTimeSeconds;
        DelayFeedback = DefaultDelayFeedback;

        Lfo1.ResetToDefaults();
        Lfo2.ResetToDefaults();

        foreach (OscillatorParameters oscillator in _oscillators)
        {
            oscillator.ResetToDefaults();
        }

        foreach (ModulationRoute route in _modulationRoutes)
        {
            route.Source = ModulationSource.None;
            route.Destination = ModulationDestination.None;
            route.Amount = 0f;
            route.OscillatorIndex = -1;
        }
    }

    public OscillatorParameters GetOscillator(int index)
    {
        return _oscillators[Math.Clamp(index, 0, _oscillators.Length - 1)];
    }

    public ModulationRoute GetModulationRoute(int index)
    {
        return _modulationRoutes[Math.Clamp(index, 0, _modulationRoutes.Length - 1)];
    }
}

internal sealed class OscillatorParameters
{
    private const bool DefaultEnabled = true;
    private const float DefaultGain = 0.80f;
    private const float DefaultDetuneCents = 0f;
    private const float DefaultGlideSeconds = 0f;
    private const float DefaultVibratoDepthCents = 0f;
    private const float DefaultVibratoRateHz = 5f;
    private const float DefaultPulseWidth = 0.50f;
    private const float DefaultPwmRateHz = 0f;
    private const float DefaultPan = 0f;
    private const float DefaultAttackSeconds = 0.05f;
    private const float DefaultDecaySeconds = 0.18f;
    private const float DefaultSustainLevel = 0.72f;
    private const float DefaultReleaseSeconds = 0.30f;

    public bool Enabled { get; set; } = DefaultEnabled;

    public Waveform Waveform { get; set; } = Waveform.Sine;

    public float Gain { get; set; } = DefaultGain;

    public float DetuneCents { get; set; } = DefaultDetuneCents;

    public float GlideSeconds { get; set; } = DefaultGlideSeconds;

    public float VibratoDepthCents { get; set; } = DefaultVibratoDepthCents;

    public float VibratoRateHz { get; set; } = DefaultVibratoRateHz;

    public float PulseWidth { get; set; } = DefaultPulseWidth;

    public float PwmRateHz { get; set; } = DefaultPwmRateHz;

    public float Pan { get; set; } = DefaultPan;

    public EnvelopeMode EnvelopeMode { get; set; } = EnvelopeMode.Sustain;

    public float AttackSeconds { get; set; } = DefaultAttackSeconds;

    public float DecaySeconds { get; set; } = DefaultDecaySeconds;

    public float SustainLevel { get; set; } = DefaultSustainLevel;

    public float ReleaseSeconds { get; set; } = DefaultReleaseSeconds;

    public void ResetToDefaults()
    {
        Enabled = DefaultEnabled;
        Waveform = Waveform.Sine;
        Gain = DefaultGain;
        DetuneCents = DefaultDetuneCents;
        GlideSeconds = DefaultGlideSeconds;
        VibratoDepthCents = DefaultVibratoDepthCents;
        VibratoRateHz = DefaultVibratoRateHz;
        PulseWidth = DefaultPulseWidth;
        PwmRateHz = DefaultPwmRateHz;
        Pan = DefaultPan;
        EnvelopeMode = EnvelopeMode.Sustain;
        AttackSeconds = DefaultAttackSeconds;
        DecaySeconds = DefaultDecaySeconds;
        SustainLevel = DefaultSustainLevel;
        ReleaseSeconds = DefaultReleaseSeconds;
    }
}
