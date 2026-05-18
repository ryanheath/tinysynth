namespace TinySynth.Synth;

internal sealed record GmPreset(
    GmInstrumentFamily Family,
    string Name,
    string Description,
    Action<SynthParameters> Apply);

internal static class GmPresetCatalog
{
    private static readonly IReadOnlyList<GmPreset> _presets =
    [
        CreatePreset(GmInstrumentFamily.Piano, "Grand Piano", "Bright layered attack with short room ambience.", p => ConfigurePiano(p, bright: true, electric: false)),
        CreatePreset(GmInstrumentFamily.Piano, "Honky-Tonk", "Detuned midrange piano with a lively room.", p => ConfigurePiano(p, bright: false, electric: true)),
        CreatePreset(GmInstrumentFamily.ChromaticPercussion, "Celesta", "Bell-like sine shimmer with soft reverb.", p => ConfigureChromatic(p, metallic: false, vibraphone: false)),
        CreatePreset(GmInstrumentFamily.ChromaticPercussion, "Vibraphone", "Rounded struck tone with chorus and tremolo.", p => ConfigureChromatic(p, metallic: true, vibraphone: true)),
        CreatePreset(GmInstrumentFamily.Organ, "Drawbar Organ", "Full-bodied organ with chorus and long sustain.", p => ConfigureOrgan(p, church: false)),
        CreatePreset(GmInstrumentFamily.Organ, "Church Organ", "Wide sustained organ with hall bloom.", p => ConfigureOrgan(p, church: true)),
        CreatePreset(GmInstrumentFamily.Guitar, "Nylon Guitar", "Warm plucked guitar with soft attack and room.", p => ConfigureGuitar(p, electric: false, muted: false)),
        CreatePreset(GmInstrumentFamily.Guitar, "Muted Guitar", "Short damped pluck with filtered attack.", p => ConfigureGuitar(p, electric: true, muted: true)),
        CreatePreset(GmInstrumentFamily.Bass, "Finger Bass", "Rounded electric bass with subtle growl.", p => ConfigureBass(p, synth: false)),
        CreatePreset(GmInstrumentFamily.Bass, "Synth Bass", "Punchy low-end synth bass with filter snap.", p => ConfigureBass(p, synth: true)),
        CreatePreset(GmInstrumentFamily.Strings, "Violin Section", "Focused bowed strings with gentle chorus.", p => ConfigureStrings(p, solo: true)),
        CreatePreset(GmInstrumentFamily.Strings, "Pizzicato", "Short plucked strings with woody decay.", p => ConfigureStrings(p, solo: false)),
        CreatePreset(GmInstrumentFamily.Ensemble, "Slow Strings", "Layered ensemble pad with hall reverb.", p => ConfigureEnsemble(p, choir: false)),
        CreatePreset(GmInstrumentFamily.Ensemble, "Choir Aahs", "Airy vocal pad with shimmer and softness.", p => ConfigureEnsemble(p, choir: true)),
        CreatePreset(GmInstrumentFamily.Brass, "Trumpet", "Brassy lead with a quick bite and body.", p => ConfigureBrass(p, mellow: false)),
        CreatePreset(GmInstrumentFamily.Brass, "French Horn", "Rounded orchestral brass with deeper resonance.", p => ConfigureBrass(p, mellow: true)),
        CreatePreset(GmInstrumentFamily.Reed, "Alto Sax", "Breathy reed tone with gentle vibrato.", p => ConfigureReed(p, clarinet: false)),
        CreatePreset(GmInstrumentFamily.Reed, "Clarinet", "Woody focused reed with softer highs.", p => ConfigureReed(p, clarinet: true)),
        CreatePreset(GmInstrumentFamily.Pipe, "Flute", "Pure airy flute with subtle motion.", p => ConfigurePipe(p, panFlute: false)),
        CreatePreset(GmInstrumentFamily.Pipe, "Pan Flute", "Breathy pan flute with delayed tail.", p => ConfigurePipe(p, panFlute: true)),
        CreatePreset(GmInstrumentFamily.SynthLead, "Lead Saw", "Classic bright synth lead with slight delay.", p => ConfigureLead(p, square: false)),
        CreatePreset(GmInstrumentFamily.SynthLead, "Pulse Lead", "Animated PWM lead with chorus width.", p => ConfigureLead(p, square: true)),
        CreatePreset(GmInstrumentFamily.SynthPad, "Warm Pad", "Smooth analog pad with chorus and hall.", p => ConfigurePad(p, airy: false)),
        CreatePreset(GmInstrumentFamily.SynthPad, "Halo Pad", "Airy pad with shimmer and long release.", p => ConfigurePad(p, airy: true)),
        CreatePreset(GmInstrumentFamily.SynthEffects, "Atmosphere", "Moving texture with noise wash and delay.", p => ConfigureSynthFx(p, beam: false)),
        CreatePreset(GmInstrumentFamily.SynthEffects, "Sci-Fi Beam", "Sweeping resonant effect with pinging tail.", p => ConfigureSynthFx(p, beam: true)),
        CreatePreset(GmInstrumentFamily.Ethnic, "Sitar", "Plucked string with buzzing brightness and delay.", p => ConfigureEthnic(p, plucked: true)),
        CreatePreset(GmInstrumentFamily.Ethnic, "Shamisen", "Sharp percussive string with short body.", p => ConfigureEthnic(p, plucked: false)),
        CreatePreset(GmInstrumentFamily.Percussive, "Timpani", "Deep mallet hit with resonant decay.", p => ConfigurePercussive(p, tuned: true)),
        CreatePreset(GmInstrumentFamily.Percussive, "Taiko", "Boomy drum hit with filtered noise thump.", p => ConfigurePercussive(p, tuned: false)),
        CreatePreset(GmInstrumentFamily.SoundEffects, "Rain", "Filtered noise with spacious ambience.", p => ConfigureSoundFx(p, burst: false)),
        CreatePreset(GmInstrumentFamily.SoundEffects, "Helicopter", "Chopped noise wash with PWM rotor feel.", p => ConfigureSoundFx(p, burst: true))
    ];

    public static IReadOnlyList<GmInstrumentFamily> Families { get; } = Enum.GetValues<GmInstrumentFamily>();

    private static readonly IReadOnlyDictionary<GmInstrumentFamily, IReadOnlyList<GmPreset>> _presetsByFamily =
        Families.ToDictionary(
            static family => family,
            static family => (IReadOnlyList<GmPreset>)_presets.Where(p => p.Family == family).ToArray());

    public static IReadOnlyList<GmPreset> Presets => _presets;

    public static IReadOnlyList<GmPreset> GetPresets(GmInstrumentFamily family)
    {
        return _presetsByFamily[family];
    }

    public static void ApplyPreset(GmPreset preset, SynthParameters parameters)
    {
        parameters.ResetToDefaults();
        preset.Apply(parameters);
    }

    private static GmPreset CreatePreset(GmInstrumentFamily family, string name, string description, Action<SynthParameters> apply)
    {
        return new GmPreset(family, name, description, apply);
    }

    private static void ConfigurePiano(SynthParameters p, bool bright, bool electric)
    {
        ConfigureOsc(p, 0, Waveform.Sine, bright ? 0.65f : 0.55f, 0f, 0.002f, 0.08f, 0.22f, 0.10f, 0.90f, 0.20f, pan: -0.10f, envelopeMode: EnvelopeMode.OneShot);
        ConfigureOsc(p, 1, electric ? Waveform.Triangle : Waveform.Saw, 0.32f, electric ? 7f : 2f, 0f, 0.01f, 0.35f, 0.00f, 0.75f, 0.22f, pan: 0.12f, envelopeMode: EnvelopeMode.OneShot);
        ConfigureOsc(p, 2, Waveform.Noise, bright ? 0.06f : 0.03f, 0f, 0f, 0.001f, 0.05f, 0.00f, 0.00f, 0.05f, envelopeMode: EnvelopeMode.OneShot);
        DisableOsc(p, 3);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = bright ? 7600f : 4600f;
        p.FilterResonance = electric ? 0.28f : 0.10f;
        ConfigureRoute(p, 0, ModulationSource.Envelope, ModulationDestination.FilterCutoff, bright ? 0.35f : 0.20f);
        p.ReverbType = ReverbType.Room;
        p.ReverbMix = electric ? 0.18f : 0.12f;
        p.ReverbSize = 0.30f;
        p.ReverbDamping = 0.38f;
        if (electric)
        {
            p.ChorusType = ChorusType.Light;
            p.ChorusMix = 0.14f;
            p.ChorusRateHz = 0.7f;
            p.ChorusDepth = 0.20f;
        }
    }

    private static void ConfigureChromatic(SynthParameters p, bool metallic, bool vibraphone)
    {
        ConfigureOsc(p, 0, Waveform.Sine, 0.68f, 0f, vibraphone ? 15f : 0f, 0.004f, 0.80f, 0.00f, 0.00f, 1.40f, pan: vibraphone ? -0.18f : -0.08f, envelopeMode: EnvelopeMode.OneShot);
        ConfigureOsc(p, 1, metallic ? Waveform.Metallic : Waveform.Sine, metallic ? 0.22f : 0.28f, 12f, vibraphone ? 0f : 0f, 0.002f, 0.65f, 0.00f, 0.00f, 1.10f, pan: vibraphone ? 0.18f : 0.08f, envelopeMode: EnvelopeMode.OneShot);
        DisableOsc(p, 2);
        DisableOsc(p, 3);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = metallic ? 5200f : 4800f;
        p.FilterResonance = metallic ? 0.14f : 0.18f;
        p.ReverbType = ReverbType.Hall;
        p.ReverbMix = 0.26f;
        p.ReverbSize = 0.62f;
        p.ReverbDamping = 0.22f;
        if (vibraphone)
        {
            p.ChorusType = ChorusType.Light;
            p.ChorusMix = 0.12f;
            p.ChorusRateHz = 1.6f;
            p.ChorusDepth = 0.18f;
            p.ChorusTremoloDepth = 0.26f;
        }
    }

    private static void ConfigureOrgan(SynthParameters p, bool church)
    {
        ConfigureOsc(p, 0, Waveform.Organ, church ? 0.34f : 0.38f, 0f, 0f, 0.01f, 0.08f, 0.90f, 0.85f, 0.50f, pan: -0.32f);
        ConfigureOsc(p, 1, Waveform.Sine, 0.28f, 1200f, 0f, 0.01f, 0.06f, 0.88f, 0.82f, 0.52f);
        ConfigureOsc(p, 2, Waveform.Organ, church ? 0.16f : 0.18f, -1200f, 0f, 0.01f, 0.07f, 0.86f, 0.80f, 0.48f, pan: 0.32f);
        DisableOsc(p, 3);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = church ? 3000f : 3600f;
        p.FilterResonance = church ? 0.12f : 0.14f;
        p.ChorusType = church ? ChorusType.Wide : ChorusType.Ensemble;
        p.ChorusMix = church ? 0.16f : 0.20f;
        p.ChorusRateHz = 0.35f;
        p.ChorusDepth = church ? 0.40f : 0.26f;
        p.ChorusTremoloDepth = church ? 0.12f : 0.08f;
        p.ReverbType = church ? ReverbType.Hall : ReverbType.Room;
        p.ReverbMix = church ? 0.30f : 0.14f;
        p.ReverbSize = church ? 0.82f : 0.35f;
        p.ReverbDamping = 0.32f;
    }

    private static void ConfigureGuitar(SynthParameters p, bool electric, bool muted)
    {
        ConfigureOsc(p, 0, electric ? Waveform.Saw : Waveform.Triangle, 0.48f, 0f, 0f, 0.002f, muted ? 0.10f : 0.18f, 0.00f, muted ? 0.12f : 0.22f, muted ? 0.08f : 0.25f, envelopeMode: EnvelopeMode.OneShot);
        ConfigureOsc(p, 1, Waveform.Sine, 0.26f, 7f, 0f, 0.001f, 0.10f, 0.00f, 0.00f, muted ? 0.06f : 0.18f, envelopeMode: EnvelopeMode.OneShot);
        ConfigureOsc(p, 2, Waveform.Noise, muted ? 0.10f : 0.04f, 0f, 0f, 0.001f, 0.03f, 0.00f, 0.00f, 0.03f, envelopeMode: EnvelopeMode.OneShot);
        DisableOsc(p, 3);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = muted ? 2600f : (electric ? 4200f : 5200f);
        p.FilterResonance = electric ? 0.22f : 0.12f;
        p.FilterEnvelopeAmount = 0.28f;
        p.ReverbType = ReverbType.Room;
        p.ReverbMix = muted ? 0.06f : 0.12f;
        p.ReverbSize = 0.28f;
        p.ReverbDamping = 0.42f;
        if (electric)
        {
            p.DelayType = DelayType.Slap;
            p.DelayMix = 0.10f;
            p.DelayTimeSeconds = 0.16f;
            p.DelayFeedback = 0.18f;
        }
    }

    private static void ConfigureBass(SynthParameters p, bool synth)
    {
        ConfigureOsc(p, 0, synth ? Waveform.Saw : Waveform.Square, 0.58f, 0f, 0f, 0.003f, 0.12f, 0.65f, 0.52f, 0.18f, pulseWidth: synth ? 0.50f : 0.38f, pwmRate: synth ? 0f : 0.4f, pan: synth ? -0.08f : 0f);
        ConfigureOsc(p, 1, Waveform.Sine, 0.34f, -1200f, 0f, 0.003f, 0.08f, 0.70f, 0.56f, 0.22f, pan: synth ? 0.08f : 0f);
        DisableOsc(p, 2);
        DisableOsc(p, 3);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = synth ? 1400f : 2200f;
        p.FilterResonance = synth ? 0.34f : 0.18f;
        ConfigureRoute(p, 0, ModulationSource.Envelope, ModulationDestination.FilterCutoff, synth ? 0.55f : 0.18f);
        if (synth)
        {
            p.ChorusType = ChorusType.Light;
            p.ChorusMix = 0.08f;
            p.ChorusRateHz = 0.45f;
            p.ChorusDepth = 0.18f;
        }
    }

    private static void ConfigureStrings(SynthParameters p, bool solo)
    {
        ConfigureOsc(p, 0, Waveform.Saw, 0.44f, 0f, solo ? 4f : 0f, 0.04f, solo ? 0.30f : 0.05f, solo ? 0.70f : 0.00f, solo ? 0.55f : 0.00f, solo ? 0.42f : 0.22f, pan: solo ? -0.18f : -0.24f);
        ConfigureOsc(p, 1, solo ? Waveform.Triangle : Waveform.Saw, 0.28f, solo ? 7f : -7f, 0f, 0.03f, solo ? 0.24f : 0.08f, solo ? 0.66f : 0.00f, solo ? 0.50f : 0.00f, solo ? 0.38f : 0.18f, pan: solo ? 0.14f : 0.24f);
        if (solo)
        {
            ConfigureOsc(p, 2, Waveform.Sine, 0.10f, 0f, 5f, 0.05f, 0.18f, 0.60f, 0.46f, 0.34f, pan: 0.04f);
        }
        else
        {
            ConfigureOsc(p, 2, Waveform.Noise, 0.06f, 0f, 0f, 0.001f, 0.04f, 0.00f, 0.00f, 0.04f, envelopeMode: EnvelopeMode.OneShot);
        }
        DisableOsc(p, 3);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = solo ? 4800f : 3000f;
        p.FilterResonance = 0.12f;
        p.ChorusType = solo ? ChorusType.Light : ChorusType.Ensemble;
        p.ChorusMix = solo ? 0.12f : 0.22f;
        p.ChorusRateHz = 0.42f;
        p.ChorusDepth = solo ? 0.18f : 0.30f;
        p.ReverbType = ReverbType.Hall;
        p.ReverbMix = solo ? 0.16f : 0.10f;
        p.ReverbSize = 0.58f;
        p.ReverbDamping = 0.44f;
    }

    private static void ConfigureEnsemble(SynthParameters p, bool choir)
    {
        ConfigureOsc(p, 0, choir ? Waveform.Triangle : Waveform.Saw, 0.36f, 0f, 0f, 0.12f, 0.60f, 0.82f, 0.70f, 0.90f, pan: -0.34f);
        ConfigureOsc(p, 1, Waveform.Saw, 0.24f, 8f, 0f, 0.10f, 0.55f, 0.80f, 0.68f, 0.84f, pan: -0.12f);
        ConfigureOsc(p, 2, choir ? Waveform.Sine : Waveform.Triangle, 0.18f, -8f, choir ? 8f : 0f, 0.15f, 0.48f, 0.78f, 0.66f, 0.92f, pan: 0.12f);
        ConfigureOsc(p, 3, choir ? Waveform.Noise : Waveform.Saw, choir ? 0.03f : 0.14f, 0f, 0f, 0.03f, 0.30f, 0.70f, 0.58f, 0.70f, pan: 0.34f);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = choir ? 2400f : 3200f;
        p.FilterResonance = 0.10f;
        p.ChorusType = ChorusType.Wide;
        p.ChorusMix = choir ? 0.22f : 0.30f;
        p.ChorusRateHz = 0.28f;
        p.ChorusDepth = choir ? 0.26f : 0.36f;
        p.ChorusTremoloDepth = choir ? 0.12f : 0.08f;
        p.ReverbType = choir ? ReverbType.Shimmer : ReverbType.Hall;
        p.ReverbMix = choir ? 0.34f : 0.24f;
        p.ReverbSize = 0.86f;
        p.ReverbDamping = choir ? 0.26f : 0.38f;
    }

    private static void ConfigureBrass(SynthParameters p, bool mellow)
    {
        ConfigureOsc(p, 0, Waveform.Saw, mellow ? 0.46f : 0.58f, 0f, 0f, 0.01f, 0.12f, 0.76f, 0.58f, 0.24f);
        ConfigureOsc(p, 1, Waveform.Square, mellow ? 0.18f : 0.24f, mellow ? -4f : 3f, 0f, 0.01f, 0.10f, 0.70f, 0.54f, 0.20f, pulseWidth: mellow ? 0.42f : 0.48f, pwmRate: mellow ? 0.20f : 0f);
        ConfigureOsc(p, 2, Waveform.Noise, mellow ? 0.02f : 0.04f, 0f, 0f, 0.001f, 0.04f, 0.00f, 0.00f, 0.04f);
        DisableOsc(p, 3);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = mellow ? 2600f : 3400f;
        p.FilterResonance = mellow ? 0.16f : 0.24f;
        ConfigureRoute(p, 0, ModulationSource.Envelope, ModulationDestination.FilterCutoff, 0.45f);
        p.ReverbType = ReverbType.Room;
        p.ReverbMix = mellow ? 0.14f : 0.10f;
        p.ReverbSize = 0.34f;
        p.ReverbDamping = 0.44f;
    }

    private static void ConfigureReed(SynthParameters p, bool clarinet)
    {
        ConfigureOsc(p, 0, clarinet ? Waveform.Square : Waveform.Saw, 0.46f, 0f, clarinet ? 8f : 14f, 0.03f, 0.15f, 0.74f, 0.62f, 0.18f, pulseWidth: clarinet ? 0.34f : 0.50f, pwmRate: clarinet ? 0.25f : 0f);
        ConfigureOsc(p, 1, Waveform.Triangle, 0.20f, clarinet ? 2f : -3f, clarinet ? 6f : 10f, 0.04f, 0.14f, 0.68f, 0.56f, 0.20f);
        ConfigureOsc(p, 2, Waveform.Noise, clarinet ? 0.01f : 0.05f, 0f, 0f, 0.001f, 0.05f, 0.00f, 0.00f, 0.04f);
        DisableOsc(p, 3);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = clarinet ? 2200f : 2800f;
        p.FilterResonance = 0.22f;
        ConfigureRoute(p, 0, ModulationSource.Envelope, ModulationDestination.FilterCutoff, 0.18f);
    }

    private static void ConfigurePipe(SynthParameters p, bool panFlute)
    {
        ConfigureOsc(p, 0, Waveform.Sine, 0.62f, 0f, panFlute ? 7f : 4f, 0.02f, 0.10f, 0.82f, 0.72f, 0.22f);
        ConfigureOsc(p, 1, Waveform.Triangle, 0.16f, 3f, panFlute ? 10f : 6f, 0.03f, 0.12f, 0.70f, 0.60f, 0.24f);
        ConfigureOsc(p, 2, Waveform.Noise, panFlute ? 0.06f : 0.03f, 0f, 0f, 0.001f, 0.04f, 0.00f, 0.00f, 0.04f);
        DisableOsc(p, 3);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = panFlute ? 3600f : 5200f;
        p.FilterResonance = 0.12f;
        p.ReverbType = ReverbType.Room;
        p.ReverbMix = panFlute ? 0.18f : 0.12f;
        p.ReverbSize = 0.42f;
        p.ReverbDamping = 0.30f;
        if (panFlute)
        {
            p.DelayType = DelayType.Slap;
            p.DelayMix = 0.08f;
            p.DelayTimeSeconds = 0.22f;
            p.DelayFeedback = 0.16f;
        }
    }

    private static void ConfigureLead(SynthParameters p, bool square)
    {
        ConfigureOsc(p, 0, square ? Waveform.Pulse : Waveform.Saw, square ? 0.48f : 0.54f, 0f, 0f, 0.01f, 0.12f, 0.82f, 0.68f, 0.20f, pulseWidth: 0.34f, pwmRate: square ? 3.2f : 0f, pan: -0.10f);
        ConfigureOsc(p, 1, Waveform.Saw, 0.22f, square ? 8f : 5f, 4f, 0.02f, 0.14f, 0.78f, 0.64f, 0.18f, pan: 0.10f);
        DisableOsc(p, 2);
        DisableOsc(p, 3);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = square ? 2800f : 5400f;
        p.FilterResonance = square ? 0.18f : 0.24f;
        ConfigureRoute(p, 0, ModulationSource.Envelope, ModulationDestination.FilterCutoff, square ? 0.40f : 0.34f);
        p.DelayType = DelayType.Tape;
        p.DelayMix = square ? 0.16f : 0.12f;
        p.DelayTimeSeconds = 0.24f;
        p.DelayFeedback = 0.28f;
        if (square)
        {
            p.GetOscillator(0).PwmRateHz = 0f;
            ConfigureLfo(p.Lfo1, ModulationLfoShape.Sine, 3.2f, 0.75f);
            ConfigureRoute(p, 1, ModulationSource.Lfo1, ModulationDestination.PulseWidth, 0.38f, oscillatorIndex: 0);
            p.ChorusType = ChorusType.Light;
            p.ChorusMix = 0.10f;
            p.ChorusRateHz = 0.6f;
            p.ChorusDepth = 0.18f;
            p.ChorusTremoloDepth = 0.09f;
        }
        else
        {
            ConfigureRoute(p, 1, ModulationSource.KeyTrack, ModulationDestination.FilterCutoff, 0.20f);
        }
    }

    private static void ConfigurePad(SynthParameters p, bool airy)
    {
        ConfigureOsc(p, 0, airy ? Waveform.Triangle : Waveform.SuperSaw, airy ? 0.34f : 0.24f, 0f, 0f, 0.18f, 0.60f, 0.84f, 0.76f, 1.10f, pan: -0.30f);
        ConfigureOsc(p, 1, airy ? Waveform.Saw : Waveform.SuperSaw, airy ? 0.26f : 0.18f, 7f, 0f, 0.20f, 0.56f, 0.82f, 0.74f, 1.00f, pan: -0.10f);
        ConfigureOsc(p, 2, Waveform.Pulse, airy ? 0.10f : 0.12f, -7f, 0f, 0.18f, 0.48f, 0.78f, 0.70f, 1.08f, pulseWidth: airy ? 0.42f : 0.35f, pwmRate: airy ? 0.25f : 0.55f, pan: 0.10f);
        ConfigureOsc(p, 3, airy ? Waveform.Noise : Waveform.Sine, airy ? 0.04f : 0.10f, 0f, airy ? 0f : 3f, 0.22f, 0.42f, 0.76f, 0.68f, 1.10f, pan: 0.30f);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = airy ? 2800f : 2200f;
        p.FilterResonance = airy ? 0.14f : 0.12f;
        p.GetOscillator(2).PwmRateHz = 0f;
        ConfigureLfo(p.Lfo1, ModulationLfoShape.Sine, airy ? 0.22f : 0.30f, 0.85f);
        ConfigureLfo(p.Lfo2, ModulationLfoShape.Triangle, airy ? 0.12f : 0.18f, 0.65f);
        ConfigureRoute(p, 0, ModulationSource.Lfo1, ModulationDestination.Pan, airy ? 0.18f : 0.14f);
        ConfigureRoute(p, 1, ModulationSource.Lfo2, ModulationDestination.PulseWidth, airy ? 0.20f : 0.26f, oscillatorIndex: 2);
        ConfigureRoute(p, 2, ModulationSource.Envelope, ModulationDestination.FilterCutoff, airy ? 0.16f : 0.22f);
        p.ChorusType = airy ? ChorusType.Wide : ChorusType.Ensemble;
        p.ChorusMix = airy ? 0.24f : 0.20f;
        p.ChorusRateHz = 0.22f;
        p.ChorusDepth = airy ? 0.34f : 0.28f;
        p.ChorusTremoloDepth = airy ? 0.16f : 0.10f;
        p.ReverbType = airy ? ReverbType.Shimmer : ReverbType.Hall;
        p.ReverbMix = airy ? 0.30f : 0.24f;
        p.ReverbSize = 0.88f;
        p.ReverbDamping = airy ? 0.24f : 0.36f;
    }

    private static void ConfigureSynthFx(SynthParameters p, bool beam)
    {
        ConfigureOsc(p, 0, beam ? Waveform.Metallic : Waveform.PinkNoise, beam ? 0.26f : 0.22f, 0f, beam ? 0f : 0f, 0.08f, 0.34f, 0.72f, 0.58f, 0.80f, pan: -0.24f);
        ConfigureOsc(p, 1, beam ? Waveform.Pulse : Waveform.Triangle, beam ? 0.18f : 0.20f, beam ? 12f : -8f, beam ? 5f : 0f, 0.06f, 0.40f, 0.70f, 0.54f, 0.88f, pulseWidth: 0.30f, pwmRate: beam ? 4.0f : 1.2f, pan: 0.24f);
        ConfigureOsc(p, 2, Waveform.Sine, 0.16f, beam ? -1200f : 0f, beam ? 2f : 0f, 0.12f, 0.50f, 0.68f, 0.50f, 0.92f, pan: beam ? 0.10f : 0f);
        DisableOsc(p, 3);
        p.FilterType = FilterType.BandPass;
        p.FilterCutoffHz = beam ? 1500f : 1000f;
        p.FilterResonance = beam ? 0.36f : 0.26f;
        ConfigureLfo(p.Lfo1, beam ? ModulationLfoShape.Saw : ModulationLfoShape.Sine, beam ? 1.20f : 0.30f, 1f);
        ConfigureLfo(p.Lfo2, beam ? ModulationLfoShape.Triangle : ModulationLfoShape.Sine, beam ? 0.38f : 0.18f, 0.80f);
        ConfigureRoute(p, 0, ModulationSource.Envelope, ModulationDestination.FilterCutoff, beam ? 0.80f : 0.40f);
        ConfigureRoute(p, 1, ModulationSource.Lfo1, ModulationDestination.FilterCutoff, beam ? 0.55f : 0.28f);
        ConfigureRoute(p, 2, ModulationSource.Lfo2, ModulationDestination.Pan, beam ? 0.22f : 0.12f);
        p.DelayType = beam ? DelayType.PingPong : DelayType.Tape;
        p.DelayMix = beam ? 0.22f : 0.16f;
        p.DelayTimeSeconds = beam ? 0.28f : 0.42f;
        p.DelayFeedback = beam ? 0.42f : 0.35f;
        p.ReverbType = beam ? ReverbType.Shimmer : ReverbType.Hall;
        p.ReverbMix = beam ? 0.14f : 0.24f;
        p.ReverbSize = 0.76f;
        p.ReverbDamping = beam ? 0.18f : 0.32f;
        if (!beam)
        {
            ConfigureRoute(p, 3, ModulationSource.Lfo2, ModulationDestination.ReverbMix, 0.12f);
        }
    }

    private static void ConfigureEthnic(SynthParameters p, bool plucked)
    {
        ConfigureOsc(p, 0, plucked ? Waveform.Saw : Waveform.Square, 0.46f, 0f, 0f, 0.002f, 0.14f, 0.00f, 0.22f, 0.20f, pulseWidth: plucked ? 0.50f : 0.28f, pwmRate: plucked ? 0f : 0.8f, envelopeMode: EnvelopeMode.OneShot);
        ConfigureOsc(p, 1, Waveform.Sine, 0.18f, plucked ? 7f : 14f, 0f, 0.001f, 0.10f, 0.00f, 0.00f, 0.14f, envelopeMode: EnvelopeMode.OneShot);
        ConfigureOsc(p, 2, Waveform.Noise, plucked ? 0.04f : 0.08f, 0f, 0f, 0.001f, 0.04f, 0.00f, 0.00f, 0.04f, envelopeMode: EnvelopeMode.OneShot);
        DisableOsc(p, 3);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = plucked ? 3600f : 2200f;
        p.FilterResonance = plucked ? 0.26f : 0.32f;
        p.FilterEnvelopeAmount = 0.34f;
        p.DelayType = DelayType.Slap;
        p.DelayMix = plucked ? 0.12f : 0.06f;
        p.DelayTimeSeconds = 0.18f;
        p.DelayFeedback = 0.20f;
    }

    private static void ConfigurePercussive(SynthParameters p, bool tuned)
    {
        ConfigureOsc(p, 0, tuned ? Waveform.Sine : Waveform.Triangle, tuned ? 0.74f : 0.46f, 0f, 0f, 0.001f, tuned ? 0.55f : 0.18f, 0.00f, 0.00f, tuned ? 0.80f : 0.40f, envelopeMode: EnvelopeMode.OneShot);
        ConfigureOsc(p, 1, tuned ? Waveform.Triangle : Waveform.Noise, tuned ? 0.18f : 0.30f, 12f, 0f, 0.001f, tuned ? 0.28f : 0.20f, 0.00f, 0.00f, tuned ? 0.50f : 0.24f, envelopeMode: EnvelopeMode.OneShot);
        if (tuned)
        {
            DisableOsc(p, 2);
        }
        else
        {
            ConfigureOsc(p, 2, Waveform.Sine, 0.18f, -1200f, 0f, 0.001f, 0.24f, 0.00f, 0.00f, 0.28f, envelopeMode: EnvelopeMode.OneShot);
        }
        DisableOsc(p, 3);
        p.FilterType = FilterType.LowPass;
        p.FilterCutoffHz = tuned ? 2800f : 1800f;
        p.FilterResonance = tuned ? 0.22f : 0.18f;
        p.ReverbType = tuned ? ReverbType.Room : ReverbType.Hall;
        p.ReverbMix = tuned ? 0.12f : 0.18f;
        p.ReverbSize = tuned ? 0.38f : 0.62f;
        p.ReverbDamping = 0.40f;
    }

    private static void ConfigureSoundFx(SynthParameters p, bool burst)
    {
        ConfigureOsc(p, 0, burst ? Waveform.Noise : Waveform.PinkNoise, burst ? 0.52f : 0.26f, 0f, 0f, 0.02f, burst ? 0.20f : 0.80f, burst ? 0.50f : 0.70f, burst ? 0.36f : 0.52f, burst ? 0.18f : 0.90f, pan: burst ? -0.20f : -0.12f);
        ConfigureOsc(p, 1, burst ? Waveform.Pulse : Waveform.Sine, burst ? 0.20f : 0.08f, 0f, burst ? 0f : 0f, 0.01f, 0.20f, burst ? 0.60f : 0.72f, burst ? 0.42f : 0.54f, burst ? 0.16f : 0.80f, pulseWidth: 0.24f, pwmRate: burst ? 6.4f : 0f, pan: burst ? 0.20f : 0.12f);
        DisableOsc(p, 2);
        DisableOsc(p, 3);
        p.FilterType = burst ? FilterType.BandPass : FilterType.HighPass;
        p.FilterCutoffHz = burst ? 950f : 3200f;
        p.FilterResonance = burst ? 0.34f : 0.18f;
        ConfigureLfo(p.Lfo1, burst ? ModulationLfoShape.Square : ModulationLfoShape.Sine, burst ? 5.5f : 0.24f, 1f);
        ConfigureRoute(p, 0, ModulationSource.Lfo1, ModulationDestination.FilterCutoff, burst ? 0.22f : 0.16f);
        ConfigureRoute(p, 1, ModulationSource.Lfo1, ModulationDestination.Pan, burst ? 0.35f : 0.10f);
        p.ReverbType = burst ? ReverbType.Room : ReverbType.Hall;
        p.ReverbMix = burst ? 0.08f : 0.24f;
        p.ReverbSize = burst ? 0.26f : 0.82f;
        p.ReverbDamping = burst ? 0.44f : 0.18f;
        if (burst)
        {
            p.DelayType = DelayType.PingPong;
            p.DelayMix = 0.14f;
            p.DelayTimeSeconds = 0.12f;
            p.DelayFeedback = 0.20f;
            ConfigureRoute(p, 2, ModulationSource.Lfo1, ModulationDestination.DelayMix, 0.10f);
        }
        else
        {
            ConfigureLfo(p.Lfo2, ModulationLfoShape.Triangle, 0.08f, 0.60f);
            ConfigureRoute(p, 2, ModulationSource.Lfo2, ModulationDestination.ReverbMix, 0.08f);
        }
    }

    private static void ConfigureLfo(ModulationLfoParameters lfo, ModulationLfoShape shape, float rateHz, float depth)
    {
        lfo.Shape = shape;
        lfo.RateHz = rateHz;
        lfo.Depth = depth;
    }

    private static void ConfigureRoute(SynthParameters parameters, int routeIndex, ModulationSource source, ModulationDestination destination, float amount, int oscillatorIndex = -1)
    {
        ModulationRoute route = parameters.GetModulationRoute(routeIndex);
        route.Source = source;
        route.Destination = destination;
        route.Amount = amount;
        route.OscillatorIndex = oscillatorIndex;
    }

    private static void ConfigureOsc(
        SynthParameters parameters,
        int index,
        Waveform waveform,
        float gain,
        float detuneCents,
        float vibratoDepthCents,
        float attackSeconds,
        float decaySeconds,
        float sustainLevel,
        float releaseSeconds,
        float glideSeconds,
        float pulseWidth = 0.50f,
        float pwmRate = 0f,
        float pan = 0f,
        EnvelopeMode envelopeMode = EnvelopeMode.Sustain)
    {
        OscillatorParameters osc = parameters.GetOscillator(index);
        osc.Enabled = true;
        osc.Waveform = waveform;
        osc.Gain = gain;
        osc.DetuneCents = detuneCents;
        osc.VibratoDepthCents = vibratoDepthCents;
        osc.VibratoRateHz = vibratoDepthCents > 0f ? 5.2f : 0f;
        osc.AttackSeconds = attackSeconds;
        osc.DecaySeconds = decaySeconds;
        osc.SustainLevel = sustainLevel;
        osc.ReleaseSeconds = releaseSeconds;
        osc.GlideSeconds = glideSeconds;
        osc.PulseWidth = pulseWidth;
        osc.PwmRateHz = pwmRate;
        osc.Pan = pan;
        osc.EnvelopeMode = envelopeMode;
    }

    private static void DisableOsc(SynthParameters parameters, int index)
    {
        OscillatorParameters osc = parameters.GetOscillator(index);
        osc.Enabled = false;
        osc.Gain = 0f;
        osc.DetuneCents = 0f;
        osc.VibratoDepthCents = 0f;
        osc.VibratoRateHz = 0f;
        osc.GlideSeconds = 0f;
        osc.PwmRateHz = 0f;
        osc.Pan = 0f;
        osc.EnvelopeMode = EnvelopeMode.Sustain;
    }
}
